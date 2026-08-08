using Pathly_Models;

namespace PathlyInterfaces
{
    public interface IaiResponseRepositoryInterface : IGenericInterface<AiResponse>
    {
        /// <summary>
        /// Returns the most recent AiResponse that was generated for the given subject-set
        /// fingerprint, if one exists, so the caller can reuse it instead of calling an LLM.
        /// </summary>
        Task<AiResponse?> FindMostRecentBySubjectSetHashAsync(string subjectSetHash);
    }
}
