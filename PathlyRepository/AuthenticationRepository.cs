using Microsoft.EntityFrameworkCore;
using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class AuthenticationRepository : GenericRepository<ApplicationUser>, IAuthenticationRepository
    {
        private readonly ApplicationDbContext _Context;

        public AuthenticationRepository(ApplicationDbContext context) : base(context)
        {
            _Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ApplicationUser?> GetTheUserByEmail(string email)
        {
            return await _Context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
