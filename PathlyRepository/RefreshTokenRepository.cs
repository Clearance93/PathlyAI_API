using Microsoft.EntityFrameworkCore;
using Pathly_Data;
using Pathly_Interfaces;
using Pathly_Models;

namespace PathlyRepository
{
    public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepositoryInterface
    {
        private readonly ApplicationDbContext _context;

        public RefreshTokenRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByHashAsync(string tokenHash)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
            {
                return null;
            }

            return await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        }

        public async Task<List<RefreshToken>> GetActiveForUserAsync(string applicationUserId)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
            {
                return new List<RefreshToken>();
            }

            return await _context.RefreshTokens
                .Where(t => t.ApplicationUserId == applicationUserId && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow)
                .ToListAsync();
        }
    }
}
