using Pathly_DTOs;
using Pathly_Models;

namespace Pathly_Interfaces.IService
{
    public interface ICareerAnalysisService
    {
        /// <summary>
        /// Full academic-only analysis pipeline for an uploaded transcript. Optionally binds the
        /// extracted academic record to the uploading account (<paramref name="applicationUserId"/>),
        /// which is null for anonymous/legacy uploads.
        /// </summary>
        Task<AiResponseDto> AnalyzeAsync(string base64File, string mimeType, string? fileName, string? applicationUserId = null);
    }
}
