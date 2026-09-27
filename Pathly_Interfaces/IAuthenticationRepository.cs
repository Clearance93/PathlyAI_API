using Pathly_Models;

namespace Pathly_Interfaces
{
    public interface IAuthenticationRepository : IGenericInterface<ApplicationUser>
    {
        Task<ApplicationUser?> GetTheUserByEmail(string email);

        Task<ApplicationUser?> GetByUserIdAsync(string userId);
    }
}
