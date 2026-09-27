using Pathly_Models;

namespace Pathly_Interfaces
{
    public interface IPsychometricAssessmentRepositoryInterface : IGenericInterface<PsychometricAssessment>
    {
        /// <summary>The user's most recent assessment submission (with its profile), if any.</summary>
        Task<PsychometricAssessment?> GetLatestByUserAsync(string applicationUserId);

        /// <summary>
        /// Returns the user's previously stored assessment with the exact same answer
        /// fingerprint, if one exists — the caller can then serve the stored result instead of
        /// persisting a duplicate submission.
        /// </summary>
        Task<PsychometricAssessment?> FindByUserAndFingerprintAsync(string applicationUserId, string resultFingerprint);

        /// <summary>Loads an assessment together with its psychometric profile.</summary>
        Task<PsychometricAssessment?> GetWithProfileAsync(Guid psychometricAssessmentId);

        /// <summary>All assessments owned by the account, newest first (for POPIA export).</summary>
        Task<List<PsychometricAssessment>> GetAllForUserAsync(string applicationUserId);
    }
}
