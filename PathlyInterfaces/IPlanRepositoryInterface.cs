using Pathly_Models;

namespace PathlyInterfaces
{
    public interface IPlanRepositoryInterface : IGenericInterface<Plan>
    {
        Task<Plan?> GetByCodeAsync(string code);

        Task<IEnumerable<Plan>> GetActivePlansAsync();
    }
}
