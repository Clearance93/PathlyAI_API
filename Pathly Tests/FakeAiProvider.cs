using Pathly_DTOs;
using PathlyInterfaces.IService;

namespace Pathly_Tests
{
    /// <summary>
    /// Minimal hand-rolled test double implementing both provider marker interfaces, so
    /// Groq/Router failover can be tested without a mocking library.
    /// </summary>
    public class FakeAiProvider : IPrimaryCareerAiProvider, IFallbackCareerAiProvider
    {
        public int CallCount { get; private set; }

        public Func<AiResponseDto>? ResponseFactory { get; set; }

        public Exception? ExceptionToThrow { get; set; }

        public Task<AiResponseDto> AnalyzeAcademicRecordAsync(ExtractedAcademicRecordDto academicRecord, ApsResultDto apsResult)
        {
            return AnalyzeAcademicRecordAsync(academicRecord, apsResult, null, null);
        }

        public Task<AiResponseDto> AnalyzeAcademicRecordAsync(
            ExtractedAcademicRecordDto academicRecord,
            ApsResultDto apsResult,
            List<CareerEvidenceDto>? careerEvidence,
            PsychometricProfileDto? psychometricProfile)
        {
            CallCount++;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(ResponseFactory?.Invoke() ?? new AiResponseDto());
        }

        public static AiResponseDto UsableResponse()
        {
            return new AiResponseDto
            {
                Summary = "A usable analysis.",
                ApsAnalysis = new ApsAnalysisDto { CalculatedAps = 30 }
            };
        }

        public static AiResponseDto UnusableResponse()
        {
            // No Summary and no ApsAnalysis — matches ResilientCareerAiService's "unusable" check.
            return new AiResponseDto();
        }
    }
}
