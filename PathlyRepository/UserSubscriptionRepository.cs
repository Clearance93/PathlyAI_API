using Microsoft.EntityFrameworkCore;
using Pathly_Data;
using Pathly_Enums;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class UserSubscriptionRepository : GenericRepository<UserSubscription>, IUserSubscriptionRepositoryInterface
    {
        private readonly ApplicationDbContext _Context;

        public UserSubscriptionRepository(ApplicationDbContext context) : base(context)
        {
            _Context = context;
        }

        public async Task<UserSubscription?> GetActiveForUserAsync(string userId)
        {
            var now = DateTime.UtcNow;

            return await _Context.UserSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId &&
                            s.Status == SubscriptionStatus.Active &&
                            (s.CurrentPeriodEndUtc == null || s.CurrentPeriodEndUtc > now))
                .OrderByDescending(s => s.StartedAtUtc)
                .FirstOrDefaultAsync();
        }

        public async Task<UserSubscription?> GetByProviderRefAsync(string providerSubscriptionRef)
        {
            if (string.IsNullOrWhiteSpace(providerSubscriptionRef))
            {
                return null;
            }

            return await _Context.UserSubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.ProviderSubscriptionRef == providerSubscriptionRef);
        }
    }
}
