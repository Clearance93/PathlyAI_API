using Pathly_Enums;

namespace Pathly_Models
{
    /// <summary>
    /// One attempted charge against the payment provider. Created before redirecting the user
    /// to checkout and reconciled by webhook so status is never trusted from the browser.
    /// </summary>
    public class PaymentTransaction
    {
        public Guid PaymentTransactionId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? ApplicationUser { get; set; }

        public Guid? PlanId { get; set; }

        public Plan? Plan { get; set; }

        public PaymentPurpose Purpose { get; set; }

        public long AmountInCents { get; set; }

        public string Currency { get; set; } = "ZAR";

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        /// <summary>The provider we charged through (e.g. "paystack").</summary>
        public string Provider { get; set; } = "paystack";

        /// <summary>Our unique reference sent to the provider — webhooks reconcile on this.</summary>
        public string Reference { get; set; } = string.Empty;

        public string? ProviderTransactionRef { get; set; }

        public string? FailureReason { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAtUtc { get; set; }
    }
}
