using Pathly_Models;

namespace PathlyInterfaces
{
    public interface IAuthenticationRepository : IGenericInterface<ApplicationUser>
    {
        Task<ApplicationUser?> GetTheUserByEmail(string email);
    }
}
