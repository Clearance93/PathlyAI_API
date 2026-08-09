using Pathly_DTOs;

namespace PathlyInterfaces.IService
{
    public interface IGroqService
    {
        Task<AiResponseDto> AnalyzeAcademicRecordAsync(ExtractedAcademicRecordDto academicRecord, ApsResultDto apsResult);

        /// <summary>
        /// Richer variant that also passes Pathly's pre-computed, deterministic career evidence
        /// (Part 9/10/11) and, for premium (Layer 2) analyses, the learner's psychometric
        /// profile (Part 7). Implementations should have the 2-arg overload delegate here with
        /// nulls so existing simple callers are unaffected.
        /// </summary>
        Task<AiResponseDto> AnalyzeAcademicRecordAsync(
            ExtractedAcademicRecordDto academicRecord,
            ApsResultDto apsResult,
            List<CareerEvidenceDto>? careerEvidence,
            PsychometricProfileDto? psychometricProfile);
    }
}
