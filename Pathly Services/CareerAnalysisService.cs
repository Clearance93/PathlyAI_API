using AutoMapper;
using Pathly_Core.Unit;
using Pathly_DTOs;
using Pathly_Helper;
using Pathly_Models;
using PathlyInterfaces.IService;
using System.Text.Json;

namespace Pathly_Services
{
    public class CareerAnalysisService : ICareerAnalysisService
    {
        private readonly IDocumentExtractionService _ExtractionService;
        private readonly IApsCalculationService _ApsCalculation;
        private readonly IGroqService _Groq;
        private readonly IMapper _Mapper;
        private readonly IUnitOfWork _Unit;

        public CareerAnalysisService(IDocumentExtractionService extractionService,
                                    IApsCalculationService apsCalculation,
                                    IGroqService groq,
                                    IMapper mapper,
                                    IUnitOfWork unit)
        {
            _ExtractionService = extractionService ?? throw new ArgumentNullException(nameof(extractionService));
            _ApsCalculation = apsCalculation ?? throw new ArgumentNullException(nameof(_ApsCalculation));
            _Groq = groq ?? throw new ArgumentNullException(nameof(groq));
            _Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _Unit = unit ?? throw new ArgumentNullException(nameof(unit));
        }

        public async Task<AiResponseDto> AnalyzeAsync(string base64File, string mimeType, string? fileName)
        {
            var academicRecord = await _ExtractionService.ExtractAcademicRecordAsync(base64File, mimeType, fileName);

            Console.WriteLine($"Extracted subjects: {academicRecord.Subjects.Count}");
            foreach (var subject in academicRecord.Subjects)
            {
                Console.WriteLine($"Subject: {subject.SubjectName} | Mark: {subject.NumericMark} | Symbol: {subject.Symbol}");
            }

            // Persist every extraction, independent of whether we end up hitting an LLM.
            // This is our own dataset going forward, and it's what the subject-set cache
            // lookup below is keyed against.
            await PersistExtractedRecordAsync(academicRecord);

            var apsResult = _ApsCalculation.CalculateAPS(academicRecord.Subjects);

            Console.WriteLine($"Calculated APS: {apsResult}");

            var subjectSetHash = AcademicRecordFingerprint.ComputeHash(academicRecord);

            var (aiResponse, servedFromCache) = await GetAiResponseAsync(academicRecord, apsResult, subjectSetHash);

            ReconcileApsAnalysis(aiResponse, apsResult);

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

                SubjectSetHash = subjectSetHash,

                AddedAt = DateTime.Now,
                TimeStamp = DateTime.Now
            };

            await _Unit.AiResponse.AddAsync(llmResponse);
            await _Unit.SaveChangesAsync();

            Console.WriteLine(servedFromCache
                ? "AI analysis served from the database cache — no LLM call made."
                : "AI analysis generated by LLM and cached for future identical subject sets.");

            return aiResponse;
        }

        /// <summary>
        /// Saves the raw extraction (record + every subject) so it's never lost, regardless
        /// of whether we go on to hit an LLM for it.
        /// </summary>
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

        /// <summary>
        /// The money-saving step: if we've already analysed this exact subject/mark set before,
        /// reuse that stored response instead of paying for another LLM call. Only calls out to
        /// Groq (with Azure Model Router as its own internal fallback) on a genuine cache miss.
        /// </summary>
        private async Task<(AiResponseDto Response, bool ServedFromCache)> GetAiResponseAsync(
            ExtractedAcademicRecordDto academicRecord,
            ApsResultDto apsResult,
            string subjectSetHash)
        {
            var cached = await _Unit.AiResponse.FindMostRecentBySubjectSetHashAsync(subjectSetHash);

            if (cached?.ResponseJson is not null)
            {
                try
                {
                    var cachedResponse = JsonSerializer.Deserialize<AiResponseDto>(
                        cached.ResponseJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (cachedResponse is not null)
                    {
                        Console.WriteLine($"Cache hit for subject set {subjectSetHash} — skipping the LLM call.");
                        return (cachedResponse, true);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Cached response for {subjectSetHash} could not be deserialized ({ex.Message}). Falling back to a live call.");
                }
            }

            var freshResponse = await _Groq.AnalyzeAcademicRecordAsync(academicRecord, apsResult);

            return (freshResponse, false);
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