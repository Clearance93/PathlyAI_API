using Pathly_Models;

namespace Pathly_Interfaces
{
    public interface IPlanRepositoryInterface : IGenericInterface<Plan>
    {
        Task<Plan?> GetByCodeAsync(string code);

        Task<IEnumerable<Plan>> GetActivePlansAsync();
    }
}
