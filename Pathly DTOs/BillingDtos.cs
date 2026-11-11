using Pathly_Enums;

namespace Pathly_DTOs
{
    /// <summary>Public view of a sellable plan (prices are whole Rand for display simplicity).</summary>
    public class PlanDto
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public PlanAudience Audience { get; set; }

        public PlanInterval Interval { get; set; }

        public int PriceInCents { get; set; }

        public string Currency { get; set; } = "ZAR";

        public bool IncludesPremiumAnalysis { get; set; }
    }

    /// <summary>Result of initiating a checkout — the browser is redirected to AuthorizationUrl.</summary>
    public class CheckoutResponseDto
    {
        public bool Success { get; set; }

        public string? AuthorizationUrl { get; set; }

        public string? Reference { get; set; }

        /// <summary>The catalogue code of the plan being purchased/activated.</summary>
        public string? PlanCode { get; set; }

        public string? PlanName { get; set; }

        public string? Message { get; set; }
    }

    /// <summary>What the SPA needs to know before rendering the checkout page.</summary>
    public class BillingConfigDto
    {
        /// <summary>False while no payment provider credentials are configured.</summary>
        public bool PaymentsEnabled { get; set; }

        /// <summary>True only when a signed-in user may activate a plan without paying (dev only).</summary>
        public bool DevActivationEnabled { get; set; }
    }

    /// <summary>The caller's live entitlement state: current plan + month-to-date consumption.</summary>
    public class UsageSummaryDto
    {
        public string PlanCode { get; set; } = "free";

        public string PlanName { get; set; } = "Free";

        public int AnalysesUsedThisMonth { get; set; }

        public int? MonthlyAnalysisQuota { get; set; }

        public int PsychometricSubmissionsThisMonth { get; set; }

        public int? MonthlyPsychometricQuota { get; set; }

        public bool PremiumAnalysisUnlocked { get; set; }

        /// <summary>True only when the plan includes psychometrics (Premium or legacy equivalents).</summary>
        public bool PsychometricsUnlocked { get; set; }

        public int CreditBalance { get; set; }

        public DateTime? CurrentPeriodEndUtc { get; set; }
    }

    /// <summary>Thrown when the user has exhausted their plan allowance for an action.</summary>
    public class QuotaExceededException : Exception
    {
        public string RequiredPlanHint { get; }

        public QuotaExceededException(string message, string requiredPlanHint)
            : base(message)
        {
            RequiredPlanHint = requiredPlanHint;
        }
    }

    /// <summary>Thrown when a requested plan code doesn't exist or isn't active.</summary>
    public class PlanNotFoundException : Exception
    {
        public PlanNotFoundException(string message)
            : base(message)
        {
        }
    }
}
