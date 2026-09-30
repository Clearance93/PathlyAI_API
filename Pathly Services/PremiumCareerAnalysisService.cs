using AutoMapper;
using Microsoft.Extensions.Logging;
using Pathly_Core.Unit;
using Pathly_DTOs;
using Pathly_Helper;
using Pathly_Models;
using Pathly_Interfaces.IService;
using System.Text.Json;

namespace Pathly_Services
{
    /// <summary>
    /// Layer 2 (Part 6/7): premium academic + psychometric career intelligence. Reuses the same
    /// extraction/APS/subject-knowledge pipeline as Layer 1, but the cache key AND the AI prompt
    /// also incorporate the learner's exact psychometric profile (Part 13) � two learners with
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
        private readonly IAcademicPredictionService _AcademicPrediction;
        private readonly IProgressionService _Progression;
        private readonly IGroqService _Groq;
        private readonly IMapper _Mapper;
        private readonly IUnitOfWork _Unit;
        private readonly ILogger<PremiumCareerAnalysisService> _Logger;

        public PremiumCareerAnalysisService(IDocumentExtractionService extractionService,
                                    IApsCalculationService apsCalculation,
                                    ISubjectKnowledgeService subjectKnowledge,
                                    ICareerEvidenceService careerEvidence,
                                    IAcademicPredictionService academicPrediction,
                                    IProgressionService progression,
                                    IGroqService groq,
                                    IMapper mapper,
                                    IUnitOfWork unit,
                                    ILogger<PremiumCareerAnalysisService> logger)
        {
            _ExtractionService = extractionService ?? throw new ArgumentNullException(nameof(extractionService));
            _ApsCalculation = apsCalculation ?? throw new ArgumentNullException(nameof(apsCalculation));
            _SubjectKnowledge = subjectKnowledge ?? throw new ArgumentNullException(nameof(subjectKnowledge));
            _CareerEvidence = careerEvidence ?? throw new ArgumentNullException(nameof(careerEvidence));
            _AcademicPrediction = academicPrediction ?? throw new ArgumentNullException(nameof(academicPrediction));
            _Progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _Groq = groq ?? throw new ArgumentNullException(nameof(groq));
            _Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AiResponseDto> AnalyzeWithPsychometricsAsync(string base64File, string mimeType, string? fileName, PsychometricProfileDto psychometricProfile, string? applicationUserId = null)
        {
            if (psychometricProfile is null)
            {
                throw new ArgumentNullException(nameof(psychometricProfile));
            }

            var academicRecord = await _ExtractionService.ExtractAcademicRecordAsync(base64File, mimeType, fileName);
            academicRecord.ApplicationUserId = string.IsNullOrWhiteSpace(applicationUserId) ? null : applicationUserId;

            return await RunCombinedAnalysisAsync(academicRecord, psychometricProfile, applicationUserId, persistRecord: true);
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

            var storedRecord = await _Unit.ExtractedAcademicRecord.GetByIdWithHistoryAsync(extractionId)
                               ?? throw new KeyNotFoundException($"No previously uploaded academic record exists with id '{extractionAcademicRecordId}'. Upload your results first.");

            // Ownership guard: a logged-in learner may only combine their OWN academic record.
            // Treat another account's record as not found (no existence leak).
            if (!string.IsNullOrWhiteSpace(storedRecord.ApplicationUserId) &&
                !string.Equals(storedRecord.ApplicationUserId, applicationUserId, StringComparison.Ordinal))
            {
                throw new KeyNotFoundException($"No previously uploaded academic record exists with id '{extractionAcademicRecordId}'. Upload your results first.");
            }

            var academicRecord = _Mapper.Map<ExtractedAcademicRecordDto>(storedRecord);

            // The stored row may pre-date user linkage; stamp the current owner so the combined
            // analysis is filed under the correct account.
            academicRecord.ApplicationUserId = applicationUserId;

            // Stored multi-term records keep their driver snapshot inside the persisted driver
            // AcademicPeriod. If no period history exists (legacy single-snapshot row), the
            // record-level Subjects are already the driver and need no fix-up.
            ApplyDriverPeriod(academicRecord);

            return await RunCombinedAnalysisAsync(academicRecord, psychometricProfile, applicationUserId, persistRecord: false);
        }

        public async Task<AiResponseDto> AnalyzeLatestStoredWithPsychometricsAsync(string? applicationUserId)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
            {
                throw new ArgumentException("The logged-in user id is required.", nameof(applicationUserId));
            }

            var storedRecord = await _Unit.ExtractedAcademicRecord.GetLatestForUserAsync(applicationUserId)
                ?? throw new KeyNotFoundException("Upload your academic results first, then complete the career assessment to generate a combined report.");

            var latestAssessment = await _Unit.PsychometricAssessment.GetLatestByUserAsync(applicationUserId);

            var profileEntity = latestAssessment?.PsychometricProfile
                ?? await _Unit.PsychometricProfile.GetLatestByUserAsync(applicationUserId)
                ?? throw new KeyNotFoundException("Complete the career assessment first so Pathly can combine it with your academic results.");

            var academicRecord = _Mapper.Map<ExtractedAcademicRecordDto>(storedRecord);
            academicRecord.ApplicationUserId = applicationUserId;
            ApplyDriverPeriod(academicRecord);

            var psychometricProfile = new PsychometricProfileDto
            {
                PsychometricProfileId = profileEntity.PsychometricProfileId,
                Realistic = profileEntity.Realistic,
                Investigative = profileEntity.Investigative,
                Artistic = profileEntity.Artistic,
                Social = profileEntity.Social,
                Enterprising = profileEntity.Enterprising,
                Conventional = profileEntity.Conventional,
                CreatedAt = profileEntity.CreatedAt
            };

            return await RunCombinedAnalysisAsync(academicRecord, psychometricProfile, applicationUserId, persistRecord: false);
        }

        /// <summary>
        /// Selects the block that drives the analysis from a record's term history: a Final /
        /// Promotion block wins, otherwise the highest populated term ordinal.
        /// </summary>
        private static void ApplyDriverPeriod(ExtractedAcademicRecordDto academicRecord)
        {
            if (academicRecord.AcademicPeriods.Count == 0)
            {
                return;
            }

            var driverPeriod = academicRecord.AcademicPeriods
                .OrderByDescending(p => p.IsFinal)
                .ThenByDescending(p => p.Ordinal)
                .First();

            academicRecord.Subjects = driverPeriod.Subjects;
            academicRecord.DriverTermOrdinal = driverPeriod.Ordinal == 0 ? null : driverPeriod.Ordinal;
            academicRecord.DriverTermLabel = driverPeriod.Label;
            academicRecord.DriverIsFinal = driverPeriod.IsFinal;
        }

        /// <summary>Shared core for both entry points � everything after academic extraction.</summary>
        private async Task<AiResponseDto> RunCombinedAnalysisAsync(ExtractedAcademicRecordDto academicRecord, PsychometricProfileDto psychometricProfile, string? applicationUserId, bool persistRecord)
        {
            // A record supplied fresh from an upload must be written; one that was already
            // persisted (re-combined with a later psychometric assessment) must NOT be re-inserted.
            if (persistRecord)
            {
                await PersistExtractedRecordAsync(academicRecord);
            }

            // Merge every prior upload into a longitudinal series so the premium report also
            // reflects the learner's progress over time.
            academicRecord.Progression = await _Progression.BuildForUserAsync(applicationUserId ?? string.Empty);

            await _SubjectKnowledge.EnsureSubjectsPersistedAsync(academicRecord.Subjects);
            await PersistPsychometricProfileAsync(psychometricProfile, applicationUserId);

            var apsResult = CalculateGatedAps(academicRecord);

            // Same evidence engine as Layer 1, but now WITH the psychometric profile factored
            // into PsychometricFit and therefore OverallScore (Part 7/9/10).
            var careerEvidence = await _CareerEvidence.ComputeEvidenceAsync(academicRecord, apsResult, psychometricProfile);

            var psychometricFingerprint = PsychometricProfileFingerprint.ComputeHash(psychometricProfile);

            academicRecord.AcademicPrediction = _AcademicPrediction.PredictNextTerm(academicRecord);

            var subjectSetHash = AcademicRecordFingerprint.ComputeHash(
                academicRecord,
                AcademicRecordFingerprint.CurrentAnalysisVersion,
                GroqPromptBuilder.PromptVersion,
                psychometricFingerprint);

            var (aiResponse, servedFromCache) = await GetAiResponseAsync(
                academicRecord, apsResult, careerEvidence, psychometricProfile, subjectSetHash);

            ReconcileApsAnalysis(aiResponse, apsResult, academicRecord);

            // Deterministic guard: keep the roadmap on the single institution the top career names
            // (same check as Layer 1) so a premium report can never contradict itself either.
            var coherence = CareerPathCoherenceValidator.ValidateAndRepair(aiResponse);

            aiResponse.CareerEvidence = careerEvidence;
            aiResponse.PsychometricIncluded = true;
            // Premium reports already combine both layers � no upsell needed (Part 8).
            aiResponse.PsychometricUpsellMessage = null;

            // Set the record-derived extras BEFORE serialization so a reopened report keeps them.
            aiResponse.AcademicPrediction = academicRecord.AcademicPrediction;
            aiResponse.Progression = academicRecord.Progression;
            aiResponse.NeedsManualReview = academicRecord.NeedsManualReview || coherence.NeedsManualReview;
            aiResponse.ExtractionWarnings = MergeWarnings(academicRecord.ExtractionWarnings, coherence.Warnings);

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

                ApplicationUserId = academicRecord.ApplicationUserId,
                ExtractionAcademicRecordId = academicRecord.ExtractionAcademicRecordId == Guid.Empty ? null : academicRecord.ExtractionAcademicRecordId,
                DriverTermLabel = academicRecord.DriverTermLabel,

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

            // Surface the persisted identity + record-level metadata to the caller so the UI can
            // re-open this exact report and can warn when the extraction needs a human check.
            aiResponse.AiResponseId = llmResponse.AiResponseId;
            aiResponse.ExtractionAcademicRecordId = llmResponse.ExtractionAcademicRecordId;
            aiResponse.DriverTermLabel = llmResponse.DriverTermLabel;
            aiResponse.IsPremium = true;
            aiResponse.GeneratedAt = llmResponse.AddedAt;

            if (servedFromCache)
            {
                _Logger.LogInformation("Premium AI analysis served from the database cache � no LLM call made.");
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
            extractedRecordEntity.ExtractedAt = DateTime.Now;

            // Reuse the id the extraction step already generated so the DTO and the persisted row
            // share one identity (the analysis result links back to it).
            if (extractedRecordEntity.ExtractionAcademicRecordId == Guid.Empty)
            {
                extractedRecordEntity.ExtractionAcademicRecordId = Guid.NewGuid();
            }

            academicRecord.ExtractionAcademicRecordId = extractedRecordEntity.ExtractionAcademicRecordId;

            // When the record carries a full period history, the driver snapshot lives inside the
            // driver AcademicPeriod � writing the record-level Subjects as well would duplicate
            // those rows under two parents. The top-level Subjects is kept purely as the in-memory
            // driver projection for analysis; only period rows (or, for legacy single-snapshot
            // records, the record-level Subjects) are persisted.
            if (extractedRecordEntity.AcademicPeriods.Count > 0)
            {
                extractedRecordEntity.Subjects = new List<ExtractedSubject>();

                foreach (var period in extractedRecordEntity.AcademicPeriods)
                {
                    period.AcademicPeriodId = Guid.NewGuid();
                    period.ExtractedAcademicRecordId = extractedRecordEntity.ExtractionAcademicRecordId;

                    foreach (var subject in period.Subjects)
                    {
                        subject.ExtractionSubjectId = Guid.NewGuid();
                    }
                }
            }
            else
            {
                foreach (var subject in extractedRecordEntity.Subjects)
                {
                    subject.ExtractionSubjectId = Guid.NewGuid();
                }
            }

            await _Unit.ExtractedAcademicRecord.AddAsync(extractedRecordEntity);
            await _Unit.SaveChangesAsync();
        }

        private ApsResultDto CalculateGatedAps(ExtractedAcademicRecordDto academicRecord)
        {
            var isFinalDriver = GroqPromptBuilder.IsFinalQualificationDriver(academicRecord);
            var isTertiaryOrAdult = IsTertiaryOrAdult(academicRecord);

            return _ApsCalculation.CalculateAPS(
                academicRecord.Subjects,
                isFinal: isFinalDriver,
                isTertiaryOrAdult: isTertiaryOrAdult,
                studyLevel: academicRecord.StudyLevel);
        }

        private static bool IsTertiaryOrAdult(ExtractedAcademicRecordDto academicRecord)
        {
            if (string.IsNullOrWhiteSpace(academicRecord.StudyLevel))
            {
                return false;
            }

            var level = SubjectNormalizer.Normalize(academicRecord.StudyLevel);
            return level.StartsWith("n", StringComparison.Ordinal)        // N2-N6, NCV
                || level.Contains("year", StringComparison.Ordinal)        // 1st/2nd year
                || level.Contains("semester", StringComparison.Ordinal)
                || level.Contains("tv", StringComparison.Ordinal);         // TVET / NCV
        }

        private async Task PersistPsychometricProfileAsync(PsychometricProfileDto profile, string? applicationUserId)
        {
            // Same learner with an identical score set? Reuse that row instead of duplicating �
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
                        _Logger.LogDebug("Premium cache hit for {SubjectSetHash} � skipping the LLM call.", subjectSetHash);
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

        private static List<string> MergeWarnings(List<string>? existing, List<string>? additional)
        {
            var merged = new List<string>();
            if (existing is not null) merged.AddRange(existing);
            if (additional is not null) merged.AddRange(additional);
            return merged;
        }

        private void ReconcileApsAnalysis(AiResponseDto aiResponse, ApsResultDto apsResult, ExtractedAcademicRecordDto? record)
        {
            var analysis = aiResponse.ApsAnalysis;

            if (analysis is null)
            {
                return;
            }

            analysis.CalculatedAps = apsResult.TotalAps;
            analysis.ApsExplanation = _ApsCalculation.GetApsExplanation(apsResult.TotalAps);

            // For a gated (mid-year / non-final-year) driver, no hard qualification verdict may be
            // emitted: blank out the university admit lists and force qualifiesForUniversity false.
            if (record is not null && !GroqPromptBuilder.IsFinalQualificationDriver(record))
            {
                analysis.UniversitiesTheyQualifyFor = new List<UniversityQualificationDto>();
                analysis.UniversitiesTheyDoNotQualifyFor = new List<UniversityQualificationDto>();
                analysis.QualifiesForUniveisty = false;
                return;
            }

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
