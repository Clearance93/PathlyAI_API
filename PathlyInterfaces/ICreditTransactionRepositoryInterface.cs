using Pathly_Models;

namespace PathlyInterfaces
{
    public interface ICreditTransactionRepositoryInterface : IGenericInterface<CreditTransaction>
    {
        /// <summary>The user's current credit balance (sum of all ledger deltas).</summary>
        Task<int> GetBalanceAsync(string userId);
    }
}
