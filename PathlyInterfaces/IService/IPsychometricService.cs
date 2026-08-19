using Pathly_DTOs;

namespace PathlyInterfaces.IService
{
    /// <summary>
    /// Storage API for psychometric assessments: persists the questions a logged-in learner
    /// answered together with their resulting RIASEC profile, and serves previous results back.
    /// Identical repeat submissions are detected via an answer fingerprint so the stored data is
    /// reused instead of duplicated.
    /// </summary>
    public interface IPsychometricService
    {
        /// <summary>Stores a completed assessment for the logged-in user (deduped per exact answers).</summary>
        Task<PsychometricAssessmentDto> SubmitAssessmentAsync(PsychometricSubmissionDto submission);

        /// <summary>The learner's most recent stored assessment, or null if they have none.</summary>
        Task<PsychometricAssessmentDto?> GetLatestForUserAsync(string applicationUserId);
    }
}
