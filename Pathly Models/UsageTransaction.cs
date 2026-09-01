using Pathly_Enums;

namespace Pathly_Models
{
    /// <summary>
    /// Metered platform usage per user — the raw data behind cost-to-serve, quota enforcement
    /// and margin reporting. One row per billable action.
    /// </summary>
    public class UsageTransaction
    {
        public Guid UsageTransactionId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? ApplicationUser { get; set; }

        public UsageType UsageType { get; set; }

        /// <summary>Number of units consumed (normally 1 per action).</summary>
        public int Units { get; set; } = 1;

        /// <summary>Estimated provider cost in cents at time of use (for margin analytics).</summary>
        public int? EstimatedCostInCents { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
