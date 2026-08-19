using Pathly_DTOs;
using Pathly_Enums;
using Pathly_Models;

namespace PathlyInterfaces.IService
{
    /// <summary>
    /// Entitlements, metering and checkout. Every billable action goes through here so quota
    /// enforcement, usage analytics and margin reporting all share one source of truth.
    /// </summary>
    public interface IBillingServiceInterface
    {
        /// <summary>Ensures the user may perform the action now; throws QuotaExceededException otherwise.</summary>
        Task EnsureWithinQuotaAsync(string userId, UsageType usageType);

        /// <summary>Records one performed unit of usage against the user.</summary>
        Task RecordUsageAsync(string userId, UsageType usageType, int units = 1);

        /// <summary>The caller's current plan and month-to-date consumption.</summary>
        Task<UsageSummaryDto> GetUsageSummaryAsync(string userId);

        /// <summary>Starts a payment-provider checkout for the given plan code.</summary>
        Task<CheckoutResponseDto> InitiateCheckoutAsync(string userId, string planCode);

        /// <summary>All active plans in the catalogue (public pricing page data).</summary>
        Task<IEnumerable<PlanDto>> GetActivePlansAsync();

        /// <summary>
        /// Marks a payment as succeeded (idempotent) and grants whatever it bought:
        /// subscriptions become active, once-off purchases open a 30-day entitlement window,
        /// credit packs top up the ledger.
        /// </summary>
        Task<bool> MarkPaymentSucceededAsync(string reference, string? providerTransactionRef);
    }
}
