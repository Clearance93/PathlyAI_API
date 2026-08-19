using Pathly_Enums;

namespace Pathly_Models
{
    /// <summary>
    /// A sellable plan in the Pathly catalogue. Prices are stored in South African cents
    /// (integer) to avoid floating-point rounding. ProviderPlanCode holds the payment
    /// provider's recurring-plan reference (e.g. a Paystack plan code) for subscriptions.
    /// </summary>
    public class Plan
    {
        public Guid PlanId { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public PlanAudience Audience { get; set; }

        public PlanInterval Interval { get; set; }

        public int PriceInCents { get; set; }

        public string Currency { get; set; } = "ZAR";

        /// <summary>
        /// Monthly analysis allowance across academic/premium analysis (null = unlimited).
        /// Psychometric submissions are metered separately via IncludedPsychometricAssessments.
        /// </summary>
        public int? MonthlyAnalysisQuota { get; set; }

        /// <summary>Monthly psychometric submission allowance (null = unlimited).</summary>
        public int? MonthlyPsychometricQuota { get; set; }

        /// <summary>Whether this plan unlocks the premium (psychometric-combined) analysis.</summary>
        public bool IncludesPremiumAnalysis { get; set; }

        public string? ProviderPlanCode { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
