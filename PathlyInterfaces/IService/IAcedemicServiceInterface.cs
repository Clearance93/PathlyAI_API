
using Pathly_DTOs;

namespace PathlyInterfaces.IService
{
    public interface IAcedemicServiceInterface
    {
        Task<AiResponseDto> GetStudentAcademicAnalysis(string file);
    }
}
