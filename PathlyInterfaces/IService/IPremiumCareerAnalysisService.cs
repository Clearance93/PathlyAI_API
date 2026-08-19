using Pathly_DTOs;

namespace PathlyInterfaces.IService
{
    /// <summary>
    /// Layer 2 (Part 6/7): the premium academic + psychometric career analysis. Combines the
    /// same academic extraction/APS/caching pipeline as the academic-only Layer 1
    /// (<see cref="ICareerAnalysisService"/>) with a psychometric profile, using a cache key
    /// that includes the psychometric fingerprint so it never collides with academic-only or
    /// different-psychometric results (Part 13).
    /// </summary>
    public interface IPremiumCareerAnalysisService
    {
        Task<AiResponseDto> AnalyzeWithPsychometricsAsync(string base64File, string mimeType, string? fileName, PsychometricProfileDto psychometricProfile);

        /// <summary>
        /// Same combined academic + psychometric analysis, but reuses an ALREADY-extracted
        /// academic record (from a prior Layer 1 upload) instead of requiring the file again.
        /// Optionally links the stored result to the logged-in user's account id.
        /// </summary>
        Task<AiResponseDto> AnalyzeExistingRecordWithPsychometricsAsync(string extractionAcademicRecordId, string? applicationUserId, PsychometricProfileDto psychometricProfile);
    }
}
