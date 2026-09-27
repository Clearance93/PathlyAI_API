using Pathly_DTOs;

namespace Pathly_Interfaces.IService
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
        Task<AiResponseDto> AnalyzeWithPsychometricsAsync(string base64File, string mimeType, string? fileName, PsychometricProfileDto psychometricProfile, string? applicationUserId = null);

        /// <summary>
        /// Same combined academic + psychometric analysis, but reuses an ALREADY-extracted
        /// academic record (from a prior Layer 1 upload) instead of requiring the file again.
        /// Optionally links the stored result to the logged-in user's account id.
        /// </summary>
        Task<AiResponseDto> AnalyzeExistingRecordWithPsychometricsAsync(string extractionAcademicRecordId, string? applicationUserId, PsychometricProfileDto psychometricProfile);

        /// <summary>
        /// Runs the combined analysis entirely from the logged-in learner's OWN stored data —
        /// their latest uploaded academic record plus their latest psychometric assessment — so
        /// the two layers genuinely influence one report without re-uploading the document.
        /// Throws <see cref="KeyNotFoundException"/> when either piece is missing.
        /// </summary>
        Task<AiResponseDto> AnalyzeLatestStoredWithPsychometricsAsync(string? applicationUserId);
    }
}
