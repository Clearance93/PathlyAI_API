using Pathly_Enums;
using Pathly_Models;

namespace PathlyInterfaces
{
    public interface IUsageTransactionRepositoryInterface : IGenericInterface<UsageTransaction>
    {
        /// <summary>How many units of the given usage types the user consumed since the given instant.</summary>
        Task<int> CountUnitsSinceAsync(string userId, IEnumerable<UsageType> usageTypes, DateTime sinceUtc);
    }
}
