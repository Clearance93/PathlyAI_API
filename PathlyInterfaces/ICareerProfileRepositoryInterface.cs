using Pathly_Models;

namespace PathlyInterfaces
{
    public interface ICareerProfileRepositoryInterface : IGenericInterface<CareerProfile>
    {
        Task<IReadOnlyList<CareerProfile>> GetAllCareersAsync();
    }
}
