
using Pathly_DTOs;

namespace Pathly_Interfaces.IService
{
    public interface IAcedemicServiceInterface
    {
        Task<AiResponseDto> GetStudentAcademicAnalysis(string file);
    }
}
