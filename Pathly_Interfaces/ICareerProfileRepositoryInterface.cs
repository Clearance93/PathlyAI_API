using Pathly_Models;

namespace Pathly_Interfaces
{
    public interface ICareerProfileRepositoryInterface : IGenericInterface<CareerProfile>
    {
        Task<IReadOnlyList<CareerProfile>> GetAllCareersAsync();
    }
}
