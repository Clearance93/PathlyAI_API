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
    /// Layer 1 (Part 6): academic-only career analysis. Works without any psychometric data
    /// and must remain genuinely useful on its own — see Part 8's upsell message, which
    /// encourages (never demands) completing the premium psychometric layer.
    /// </summary>
    public class CareerAnalysisService : ICareerAnalysisService
    {
        private const string PsychometricUpsellMessage =
            "Your academic results show us what you may be academically prepared for. A psychometric " +
            "assessment can add another layer by helping Pathly understand your interests and preferred " +
            "ways of working. Combining both can make your career recommendations more personalized.";

        private readonly IDocumentExtractionService _ExtractionService;
        private readonly IApsCalculationService _ApsCalculation;
        private readonly ISubjectKnowledgeService _SubjectKnowledge;
        private readonly ICareerEvidenceService _CareerEvidence;
        private readonly IGroqService _Groq;
        private readonly IMapper _Mapper;
        private readonly IUnitOfWork _Unit;
        private readonly ILogger<CareerAnalysisService> _Logger;

        public CareerAnalysisService(IDocumentExtractionService extractionService,
                                    IApsCalculationService apsCalculation,
                                    ISubjectKnowledgeService subjectKnowledge,
                                    ICareerEvidenceService careerEvidence,
                                    IGroqService groq,
                                    IMapper mapper,
                                    IUnitOfWork unit,
                                    ILogger<CareerAnalysisService> logger)
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

        public async Task<AiResponseDto> AnalyzeAsync(string base64File, string mimeType, string? fileName)
        {
            var academicRecord = await _ExtractionService.ExtractAcademicRecordAsync(base64File, mimeType, fileName);

            await PersistExtractedRecordAsync(academicRecord);

            await _SubjectKnowledge.EnsureSubjectsPersistedAsync(academicRecord.Subjects);

            var apsResult = _ApsCalculation.CalculateAPS(academicRecord.Subjects);

            var careerEvidence = await _CareerEvidence.ComputeEvidenceAsync(academicRecord, apsResult);

            var subjectSetHash = AcademicRecordFingerprint.ComputeHash(
                academicRecord,
                AcademicRecordFingerprint.CurrentAnalysisVersion,
                GroqPromptBuilder.PromptVersion);

            var (aiResponse, servedFromCache) = await GetAiResponseAsync(academicRecord, apsResult, careerEvidence, subjectSetHash);

            ReconcileApsAnalysis(aiResponse, apsResult);

            aiResponse.CareerEvidence = careerEvidence;
            aiResponse.PsychometricUpsellMessage = PsychometricUpsellMessage;

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
                AnalysisVersion = AcademicRecordFingerprint.CurrentAnalysisVersion,
                PromptVersion = GroqPromptBuilder.PromptVersion,
                IsPremium = false,

                AddedAt = DateTime.Now,
                TimeStamp = DateTime.Now
            };

            await _Unit.AiResponse.AddAsync(llmResponse);
            await _Unit.SaveChangesAsync();

            if (servedFromCache)
            {
                _Logger.LogInformation("AI analysis served from the database cache — no LLM call made.");
            }
            else
            {
                _Logger.LogInformation("AI analysis generated by LLM and cached for future identical subject sets.");
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

        private async Task<(AiResponseDto Response, bool ServedFromCache)> GetAiResponseAsync(
            ExtractedAcademicRecordDto academicRecord,
            ApsResultDto apsResult,
            List<CareerEvidenceDto> careerEvidence,
            string subjectSetHash)
        {
            var cached = await _Unit.AiResponse.FindMostRecentBySubjectSetHashAsync(subjectSetHash);

            if (cached?.ResponseJson is not null &&
                cached.AnalysisVersion == AcademicRecordFingerprint.CurrentAnalysisVersion &&
                cached.PromptVersion == GroqPromptBuilder.PromptVersion)
            {
                try
                {
                    var cachedResponse = JsonSerializer.Deserialize<AiResponseDto>(cached.ResponseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (cachedResponse is not null)
                    {
                        _Logger.LogDebug("Cache hit for subject set {SubjectSetHash} — skipping the LLM call.", subjectSetHash);
                        return (cachedResponse, true);
                    }
                }
                catch (JsonException ex)
                {
                    _Logger.LogWarning(ex, "Cached response for {SubjectSetHash} could not be deserialized. Falling back to a live call.", subjectSetHash);
                }
            }

            var freshResponse = await _Groq.AnalyzeAcademicRecordAsync(academicRecord, apsResult, careerEvidence, null);

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
