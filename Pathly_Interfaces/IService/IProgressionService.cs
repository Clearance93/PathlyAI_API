using Pathly_DTOs;

namespace Pathly_Interfaces.IService
{
    /// <summary>
    /// Builds a longitudinal progression view across every academic upload an account has made,
    /// so Pathly's guidance reflects termly/yearly improvement instead of discarding everything
    /// before the most recent document.
    /// </summary>
    public interface IProgressionService
    {
        Task<ProgressionDto> BuildForUserAsync(string applicationUserId);
    }
}
