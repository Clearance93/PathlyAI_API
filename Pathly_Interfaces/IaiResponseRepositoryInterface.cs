using Pathly_Models;

namespace Pathly_Interfaces
{
    public interface IaiResponseRepositoryInterface : IGenericInterface<AiResponse>
    {
        /// <summary>
        /// Returns the most recent AiResponse that was generated for the given subject-set
        /// fingerprint, if one exists, so the caller can reuse it instead of calling an LLM.
        /// </summary>
        Task<AiResponse?> FindMostRecentBySubjectSetHashAsync(string subjectSetHash);

        /// <summary>
        /// The account's stored analyses, newest first, with their APS analysis included.
        /// Always filtered by owner — never returns another account's rows.
        /// </summary>
        Task<List<AiResponse>> GetHistoryForUserAsync(string applicationUserId, int take = 100);

        /// <summary>
        /// A single stored analysis, but ONLY when it belongs to the given account. Returns null
        /// both when the id does not exist and when it belongs to someone else (no existence leak).
        /// </summary>
        Task<AiResponse?> GetByIdForUserAsync(Guid aiResponseId, string applicationUserId);
    }
}
