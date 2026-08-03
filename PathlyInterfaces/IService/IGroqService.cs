using Pathly_DTOs;

namespace PathlyInterfaces.IService
{
    public interface IGroqService
    {
        Task<AiResponseDto> AnalyzeAcademicRecordAsync(ExtractedAcademicRecordDto academicRecord, ApsResultDto apsResult);
    }
}
