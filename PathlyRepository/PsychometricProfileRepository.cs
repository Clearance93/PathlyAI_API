using Microsoft.EntityFrameworkCore;
using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class PsychometricProfileRepository : GenericRepository<PsychometricProfile>, IPsychometricProfileRepositoryInterface
    {
        private readonly ApplicationDbContext _Context;

        public PsychometricProfileRepository(ApplicationDbContext context) : base(context)
        {
            _Context = context;
        }

        public async Task<PsychometricProfile?> GetLatestByUserAsync(string applicationUserId)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
            {
                return null;
            }

            return await _Context.PsychometricProfiles
                .Where(p => p.ApplicationUserId == applicationUserId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<PsychometricProfile?> FindLatestMatchingForUserAsync(
            string applicationUserId,
            int realistic,
            int investigative,
            int artistic,
            int social,
            int enterprising,
            int conventional)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
            {
                return null;
            }

            return await _Context.PsychometricProfiles
                .Where(p => p.ApplicationUserId == applicationUserId &&
                            p.Realistic == realistic &&
                            p.Investigative == investigative &&
                            p.Artistic == artistic &&
                            p.Social == social &&
                            p.Enterprising == enterprising &&
                            p.Conventional == conventional)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
