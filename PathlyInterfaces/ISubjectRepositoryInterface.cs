using Pathly_Models;

namespace PathlyInterfaces
{
    public interface ISubjectRepositoryInterface : IGenericInterface<Subject>
    {
        Task<Subject?> FindByNormalizedNameAsync(string normalizedName);
    }
}
