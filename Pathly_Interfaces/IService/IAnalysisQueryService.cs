using Pathly_DTOs;

namespace Pathly_Interfaces.IService
{
    /// <summary>
    /// Read-only access to a learner's OWN stored analyses. Every method takes the caller's
    /// account id and filters on it, so this is the single place the API reads persisted
    /// results from — no controller ever queries results without an owner scope.
    /// </summary>
    public interface IAnalysisQueryService
    {
        Task<AnalysisHistoryResponseDto> GetHistoryAsync(string applicationUserId);

        /// <summary>Returns the full stored report, or null when it does not exist OR is not owned by the caller.</summary>
        Task<AiResponseDto?> GetResultAsync(Guid aiResponseId, string applicationUserId);
    }
}
