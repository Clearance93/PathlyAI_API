using Microsoft.EntityFrameworkCore;
using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class PaymentTransactionRepository : GenericRepository<PaymentTransaction>, IPaymentTransactionRepositoryInterface
    {
        private readonly ApplicationDbContext _Context;

        public PaymentTransactionRepository(ApplicationDbContext context) : base(context)
        {
            _Context = context;
        }

        public async Task<PaymentTransaction?> GetByReferenceAsync(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            return await _Context.PaymentTransactions
                .Include(t => t.Plan)
                .FirstOrDefaultAsync(t => t.Reference == reference);
        }
    }
}
