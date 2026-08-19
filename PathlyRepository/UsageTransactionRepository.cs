using Microsoft.EntityFrameworkCore;
using Pathly_Data;
using Pathly_Enums;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class UsageTransactionRepository : GenericRepository<UsageTransaction>, IUsageTransactionRepositoryInterface
    {
        private readonly ApplicationDbContext _Context;

        public UsageTransactionRepository(ApplicationDbContext context) : base(context)
        {
            _Context = context;
        }

        public async Task<int> CountUnitsSinceAsync(string userId, IEnumerable<UsageType> usageTypes, DateTime sinceUtc)
        {
            var types = usageTypes.ToList();

            return await _Context.UsageTransactions
                .Where(u => u.UserId == userId && types.Contains(u.UsageType) && u.CreatedAtUtc >= sinceUtc)
                .SumAsync(u => (int?)u.Units) ?? 0;
        }
    }
}
