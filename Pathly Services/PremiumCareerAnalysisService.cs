using AutoMapper;
using Microsoft.Extensions.Logging;
using Pathly_Core.Unit;
using Pathly_DTOs;
using Pathly_Helper;
using Pathly_Models;
using PathlyInterfaces.IService;
using System.Text.Json;

namespace Pathly_Services
{
    /// <summary>
    /// Layer 2 (Part 6/7): premium academic + psychometric career intelligence. Reuses the same
    /// extraction/APS/subject-knowledge pipeline as Layer 1, but the cache key AND the AI prompt
    /// also incorporate the learner's exact psychometric profile (Part 13) — two learners with
    /// identical academics but different psychometrics never share a cached premium result.
    /// Accepts either a fresh file upload or an already-extracted academic record id, and links
    /// everything to the logged-in user when their account id is supplied.
    /// </summary>
    public class PremiumCareerAnalysisService : IPremiumCareerAnalysisService
    {
        private readonly IDocumentExtractionService _ExtractionService;
        private readonly IApsCalculationService _ApsCalculation;
        private readonly ISubjectKnowledgeService _SubjectKnowledge;
        private readonly ICareerEvidenceService _CareerEvidence;
        private readonly IGroqService _Groq;
        private readonly IMapper _Mapper;
        private readonly IUnitOfWork _Unit;
        private readonly ILogger<PremiumCareerAnalysisService> _Logger;

        public PremiumCareerAnalysisService(IDocumentExtractionService extractionService,
                                    IApsCalculationService apsCalculation,
                                    ISubjectKnowledgeService subjectKnowledge,
                                    ICareerEvidenceService careerEvidence,
                                    IGroqService groq,
                                    IMapper mapper,
                                    IUnitOfWork unit,
                                    ILogger<PremiumCareerAnalysisService> logger)
        {
            _ExtractionService = extractionService ?? throw new ArgumentNullException(nameof(extractionService));
            _ApsCalculation = apsCalculation ?? throw new ArgumentNullException(nameof(apsCalculation));
            _SubjectKnowledge = subjectKnowledge ?? throw new ArgumentNullException(nameof(subjectKnowledge));
            _CareerEvidence = careerEvidence ?? throw new ArgumentNullException(nameof(careerEvidence));
            _Groq = groq ?? throw new ArgumentNullException(nameof(groq));
            _Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AiResponseDto> AnalyzeWithPsychometricsAsync(string base64File, string mimeType, string? fileName, PsychometricProfileDto psychometricProfile)
        {
            if (psychometricProfile is null)
            {
                throw new ArgumentNullException(nameof(psychometricProfile));
            }

            var academicRecord = await _ExtractionService.ExtractAcademicRecordAsync(base64File, mimeType, fileName);

            return await RunCombinedAnalysisAsync(academicRecord, psychometricProfile, applicationUserId: null);
        }

        public async Task<AiResponseDto> AnalyzeExistingRecordWithPsychometricsAsync(string extractionAcademicRecordId, string? applicationUserId, PsychometricProfileDto psychometricProfile)
        {
            if (string.IsNullOrWhiteSpace(extractionAcademicRecordId))
            {
                throw new ArgumentException("An extracted academic record id is required.", nameof(extractionAcademicRecordId));
            }

            if (psychometricProfile is null)
            {
                throw new ArgumentNullException(nameof(psychometricProfile));
            }

            if (!Guid.TryParse(extractionAcademicRecordId, out var extractionId))
            {
                throw new ArgumentException($"'{extractionAcademicRecordId}' is not a valid academic record id.", nameof(extractionAcademicRecordId));
            }

            var storedRecord = await _Unit.ExtractedAcademicRecord.GetByIdAsync(extractionId)
                               ?? throw new KeyNotFoundException($"No previously uploaded academic record exists with id '{extractionAcademicRecordId}'. Upload your results first.");

            var academicRecord = _Mapper.Map<ExtractedAcademicRecordDto>(storedRecord);

            return await RunCombinedAnalysisAsync(academicRecord, psychometricProfile, applicationUserId);
        }

        /// <summary>Shared core for both entry points — everything after academic extraction.</summary>
        private async Task<AiResponseDto> RunCombinedAnalysisAsync(ExtractedAcademicRecordDto academicRecord, PsychometricProfileDto psychometricProfile, string? applicationUserId)
        {
            await PersistExtractedRecordAsync(academicRecord);
            await _SubjectKnowledge.EnsureSubjectsPersistedAsync(academicRecord.Subjects);
            await PersistPsychometricProfileAsync(psychometricProfile, applicationUserId);

            var apsResult = _ApsCalculation.CalculateAPS(academicRecord.Subjects);

            // Same evidence engine as Layer 1, but now WITH the psychometric profile factored
            // into PsychometricFit and therefore OverallScore (Part 7/9/10).
            var careerEvidence = await _CareerEvidence.ComputeEvidenceAsync(academicRecord, apsResult, psychometricProfile);

            var psychometricFingerprint = PsychometricProfileFingerprint.ComputeHash(psychometricProfile);

            var subjectSetHash = AcademicRecordFingerprint.ComputeHash(
                academicRecord,
                AcademicRecordFingerprint.CurrentAnalysisVersion,
                GroqPromptBuilder.PromptVersion,
                psychometricFingerprint);

            var (aiResponse, servedFromCache) = await GetAiResponseAsync(
                academicRecord, apsResult, careerEvidence, psychometricProfile, subjectSetHash);

            ReconcileApsAnalysis(aiResponse, apsResult);

            aiResponse.CareerEvidence = careerEvidence;
            // Premium reports already combine both layers — no upsell needed (Part 8).
            aiResponse.PsychometricUpsellMessage = null;

            var apsAnalysisId = Guid.NewGuid();

            var apsAnalysis = new ApsAnalysisDto
            {
                ApsAnalysisId = apsAnalysisId,
                CalculatedAps = aiResponse.ApsAnalysis!.CalculatedAps,
                ApsExplanation = aiResponse.ApsAnalysis.ApsExplanation,
                QualifiesForUniveisty = aiResponse.ApsAnalysis.QualifiesForUniveisty,
                QualificationMessage = aiResponse.ApsAnalysis.QualificationMessage,
                UniversitiesTheyQualifyFor = aiResponse.ApsAnalysis.UniversitiesTheyQualifyFor,
                UniversitiesTheyDoNotQualifyFor = aiResponse.ApsAnalysis.UniversitiesTheyDoNotQualifyFor,
                AddedAt = DateTime.Now,
            };

            var addAps = _Mapper.Map<ApsAnalysis>(apsAnalysis);

            await _Unit.ApsAnalysis.AddAsync(addAps);

            var isCacheable = IsValidForCaching(aiResponse);

            var llmResponse = new AiResponse
            {
                AiResponseId = Guid.NewGuid(),
                ApsAnalysisId = apsAnalysisId,

                UserFullName = academicRecord.StudentName,
                Grade = academicRecord.StudyLevel,

                OverallScore = aiResponse.OverallScore,
                AcademicPersonality = aiResponse.AcademicPersonality,
                Summary = aiResponse.Summary,
                FeedBack = aiResponse.FeedBack,
                MotivationalMessage = aiResponse.MotivationalMessage,
                FiveYearsOutLook = aiResponse.FiveYearsOutLook,
                SalaryRange = aiResponse.SalaryRange,
                RiskAssessment = aiResponse.RiskAssessment,
                TeacherRecommendation = aiResponse.TeacherRecommendation,
                ParentSummary = aiResponse.ParentSummary,

                UserStrength = SerializeList(aiResponse.UserStrength),
                UserWeaknesses = SerializeList(aiResponse.UserWeaknesses),
                StudyTips = SerializeList(aiResponse.StudyTips),
                ImprovementtoRoadmap = SerializeList(aiResponse.ImprovementtoRoadmap),

                SkillsToLearn = aiResponse.SkillsToLearn,
                BursariesAvailable = aiResponse.BursariesAvailable,
                UniversitiestoConsider = aiResponse.UniversitiestoConsider,

                SubjectChangeSuggestion = string.IsNullOrWhiteSpace(aiResponse.SubjectChangeSuggestion)
                    ? null
                    : new List<string> { aiResponse.SubjectChangeSuggestion },

                ResponseJson = JsonSerializer.Serialize(aiResponse),

                SubjectSetHash = isCacheable ? subjectSetHash : null,
                PsychometricHash = psychometricFingerprint,
                AnalysisVersion = AcademicRecordFingerprint.CurrentAnalysisVersion,
                PromptVersion = GroqPromptBuilder.PromptVersion,
                IsPremium = true,

                AddedAt = DateTime.Now,
                TimeStamp = DateTime.Now
            };

            await _Unit.AiResponse.AddAsync(llmResponse);
            await _Unit.SaveChangesAsync();

            if (servedFromCache)
            {
                _Logger.LogInformation("Premium AI analysis served from the database cache — no LLM call made.");
            }
            else
            {
                _Logger.LogInformation("Premium AI analysis generated by LLM and cached for future identical academic + psychometric profiles.");
            }

            return aiResponse;
        }

        private async Task PersistExtractedRecordAsync(ExtractedAcademicRecordDto academicRecord)
        {
            var extractedRecordEntity = _Mapper.Map<ExtractedAcademicRecord>(academicRecord);
            extractedRecordEntity.ExtractionAcademicRecordId = Guid.NewGuid();
            extractedRecordEntity.ExtractedAt = DateTime.Now;

            foreach (var subject in extractedRecordEntity.Subjects)
            {
                subject.ExtractionSubjectId = Guid.NewGuid();
            }

            await _Unit.ExtractedAcademicRecord.AddAsync(extractedRecordEntity);
            await _Unit.SaveChangesAsync();
        }

        private async Task PersistPsychometricProfileAsync(PsychometricProfileDto profile, string? applicationUserId)
        {
            // Same learner with an identical score set? Reuse that row instead of duplicating —
            // this is also what lets us know we can pull the cached analysis instead of paying
            // for another LLM call.
            var existing = await _Unit.PsychometricProfile.FindLatestMatchingForUserAsync(
                applicationUserId ?? string.Empty,
                profile.Realistic,
                profile.Investigative,
                profile.Artistic,
                profile.Social,
                profile.Enterprising,
                profile.Conventional);

            if (existing is not null)
            {
                profile.PsychometricProfileId = existing.PsychometricProfileId;
                return;
            }

            var entity = new PsychometricProfile
            {
                PsychometricProfileId = profile.PsychometricProfileId == Guid.Empty ? Guid.NewGuid() : profile.PsychometricProfileId,
                ApplicationUserId = string.IsNullOrWhiteSpace(applicationUserId) ? null : applicationUserId,
                Realistic = profile.Realistic,
                Investigative = profile.Investigative,
                Artistic = profile.Artistic,
                Social = profile.Social,
                Enterprising = profile.Enterprising,
                Conventional = profile.Conventional,
                CreatedAt = DateTime.Now
            };

            await _Unit.PsychometricProfile.AddAsync(entity);
            await _Unit.SaveChangesAsync();

            profile.PsychometricProfileId = entity.PsychometricProfileId;
        }

        private async Task<(AiResponseDto Response, bool ServedFromCache)> GetAiResponseAsync(
            ExtractedAcademicRecordDto academicRecord,
            ApsResultDto apsResult,
            List<CareerEvidenceDto> careerEvidence,
            PsychometricProfileDto psychometricProfile,
            string subjectSetHash)
        {
            var cached = await _Unit.AiResponse.FindMostRecentBySubjectSetHashAsync(subjectSetHash);

            if (cached?.ResponseJson is not null &&
                cached.IsPremium &&
                cached.AnalysisVersion == AcademicRecordFingerprint.CurrentAnalysisVersion &&
                cached.PromptVersion == GroqPromptBuilder.PromptVersion)
            {
                try
                {
                    var cachedResponse = JsonSerializer.Deserialize<AiResponseDto>(
                        cached.ResponseJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (cachedResponse is not null)
                    {
                        _Logger.LogDebug("Premium cache hit for {SubjectSetHash} — skipping the LLM call.", subjectSetHash);
                        return (cachedResponse, true);
                    }
                }
                catch (JsonException ex)
                {
                    _Logger.LogWarning(ex, "Cached premium response for {SubjectSetHash} could not be deserialized. Falling back to a live call.", subjectSetHash);
                }
            }

            var freshResponse = await _Groq.AnalyzeAcademicRecordAsync(academicRecord, apsResult, careerEvidence, psychometricProfile);

            return (freshResponse, false);
        }

        private static bool IsValidForCaching(AiResponseDto? response)
        {
            return response is not null
                && !string.IsNullOrWhiteSpace(response.Summary)
                && response.ApsAnalysis is not null;
        }

        private static string? SerializeList(List<string>? list)
        {
            return list is null or { Count: 0 } ? null : JsonSerializer.Serialize(list);
        }

        private void ReconcileApsAnalysis(AiResponseDto aiResponse, ApsResultDto apsResult)
        {
            var analysis = aiResponse.ApsAnalysis;

            if (analysis is null)
            {
                return;
            }

            analysis.CalculatedAps = apsResult.TotalAps;
            analysis.ApsExplanation = _ApsCalculation.GetApsExplanation(apsResult.TotalAps);

            var allUniversities = (analysis.UniversitiesTheyQualifyFor ?? new()).Concat(analysis.UniversitiesTheyDoNotQualifyFor ?? new())
                                                                                .ToList();

            if (allUniversities.Count == 0)
            {
                return;
            }

            foreach (var uni in allUniversities)
            {
                var nowQualifies = apsResult.TotalAps >= uni.MinimumAps;
                uni.Status = nowQualifies ? "Qualifies" : "Does Not Qualify";
                uni.Gap = nowQualifies ? 0 : uni.MinimumAps - apsResult.TotalAps;
            }

            analysis.UniversitiesTheyQualifyFor = allUniversities.Where(u => apsResult.TotalAps >= u.MinimumAps).ToList();
            analysis.UniversitiesTheyDoNotQualifyFor = allUniversities.Where(u => apsResult.TotalAps < u.MinimumAps).ToList();
            analysis.QualifiesForUniveisty = analysis.UniversitiesTheyQualifyFor.Count > 0;
        }
    }
}
