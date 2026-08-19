using Pathly_Enums;

namespace Pathly_Models
{
    /// <summary>
    /// Prepaid credit ledger. Positive rows grant credits (purchase/grant), negative rows spend
    /// them. The balance is the sum of Delta — an append-only ledger keeps every change auditable.
    /// </summary>
    public class CreditTransaction
    {
        public Guid CreditTransactionId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? ApplicationUser { get; set; }

        public int Delta { get; set; }

        public CreditReason Reason { get; set; }

        /// <summary>Optional link to the payment or usage event that caused this row.</summary>
        public Guid? ReferenceId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
