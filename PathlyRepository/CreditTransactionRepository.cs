using Microsoft.EntityFrameworkCore;
using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class CreditTransactionRepository : GenericRepository<CreditTransaction>, ICreditTransactionRepositoryInterface
    {
        private readonly ApplicationDbContext _Context;

        public CreditTransactionRepository(ApplicationDbContext context) : base(context)
        {
            _Context = context;
        }

        public async Task<int> GetBalanceAsync(string userId)
        {
            return await _Context.CreditTransactions
                .Where(c => c.UserId == userId)
                .SumAsync(c => (int?)c.Delta) ?? 0;
        }
    }
}
