using Pathly_Models;

namespace Pathly_Interfaces
{
    public interface ICreditTransactionRepositoryInterface : IGenericInterface<CreditTransaction>
    {
        /// <summary>The user's current credit balance (sum of all ledger deltas).</summary>
        Task<int> GetBalanceAsync(string userId);
    }
}
