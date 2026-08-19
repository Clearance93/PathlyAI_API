using Pathly_Enums;

namespace Pathly_Models
{
    /// <summary>A user's entitlement to a plan, created when a checkout succeeds.</summary>
    public class UserSubscription
    {
        public Guid UserSubscriptionId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public Guid PlanId { get; set; }

        public Plan? Plan { get; set; }

        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>When the current paid period ends; null for once-off purchases.</summary>
        public DateTime? CurrentPeriodEndUtc { get; set; }

        public bool AutoRenew { get; set; }

        /// <summary>The payment provider's subscription reference (e.g. Paystack subscription code).</summary>
        public string? ProviderSubscriptionRef { get; set; }

        public ApplicationUser? ApplicationUser { get; set; }
    }
}
