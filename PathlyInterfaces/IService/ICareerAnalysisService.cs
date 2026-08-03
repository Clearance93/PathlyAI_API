using Pathly_DTOs;
using Pathly_Models;

namespace PathlyInterfaces.IService
{
    public interface ICareerAnalysisService
    {
        Task<AiResponseDto> AnalyzeAsync(string base64File, string mimeType, string? fileName);
    }
}
