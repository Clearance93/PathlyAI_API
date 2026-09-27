using Pathly_Models;

namespace Pathly_Interfaces
{
    public interface ISubjectRepositoryInterface : IGenericInterface<Subject>
    {
        Task<Subject?> FindByNormalizedNameAsync(string normalizedName);
    }
}
