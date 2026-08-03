using Pathly_DTOs;
using Pathly_Models;

namespace PathlyInterfaces.IService
{
    public interface IDocumentExtractionService
    {
        Task<ExtractedAcademicRecordDto> ExtractAcademicRecordAsync(string base64File, string mimeType, string? fileName);
    }
}
