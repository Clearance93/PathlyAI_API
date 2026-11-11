using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pathly_Core;
using Pathly_Core.Unit;
using Pathly_DTOs;
using Pathly_Enums;
using Pathly_Models;
using Pathly_Interfaces.IService;
using Pathly_Interfaces;
using System.Security.Cryptography;

namespace Pathly_Services
{
    /// <summary>
    /// Central billing logic: what the user's plan allows (entitlements), how much they've
    /// consumed (metering) and how they pay (checkout via a pluggable gateway). Quotas count
    /// calendar-month usage; users without an active subscription fall on the free tier.
    /// </summary>
    public class BillingService : IBillingServiceInterface
    {
        private const int FreeMonthlyAnalyses = 5;
        // Free is a preview tier: psychometrics and combined reports are Premium-only.
        private const int FreeMonthlyPsychometricSubmissions = 0;
        private const string UpgradeHint = "premium";

        private readonly IUnitOfWork _Unit;
        private readonly IPaymentGatewayInterface _Gateway;
        private readonly ILogger<BillingService> _Logger;
        private readonly PaystackSettings _Paystack;
        private readonly BillingSettings _Billing;
        private readonly bool _IsDevelopment;

        public BillingService(IUnitOfWork unit,
                              IPaymentGatewayInterface gateway,
                              ILogger<BillingService> logger,
                              IOptions<PaystackSettings> paystack,
                              IOptions<BillingSettings> billing)
        {
            _Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _Gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _Paystack = paystack?.Value ?? new PaystackSettings();
            _Billing = billing?.Value ?? new BillingSettings();
            _IsDevelopment = IsDevelopmentEnvironment();
        }

        private static bool IsDevelopmentEnvironment()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                              ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>True when the gateway has credentials and can start a real checkout.</summary>
        public bool PaymentsEnabled => _Gateway.IsConfigured;

        /// <summary>
        /// True when the current environment allows activating a plan without a payment
        /// provider. Guarded by both Development and an explicit configuration flag.
        /// </summary>
        public bool DevActivationEnabled => _IsDevelopment && _Billing.AllowDevActivation;

        public async Task EnsureWithinQuotaAsync(string userId, UsageType usageType)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var plan = await ResolveEffectivePlanAsync(userId);

            if ((usageType == UsageType.PremiumAnalysis || usageType == UsageType.PsychometricSubmission) &&
                !plan.IncludesPremiumAnalysis)
            {
                throw new QuotaExceededException(
                    usageType == UsageType.PsychometricSubmission
                        ? "The Career Interest Assessment is part of the Premium plan."
                        : "The combined academic + psychometric analysis is part of the Premium plan.",
                    UpgradeHint);
            }

            var (used, quota) = usageType switch
            {
                UsageType.AcademicAnalysis or UsageType.PremiumAnalysis =>
                    (await CountSinceStartOfMonthAsync(userId, UsageType.AcademicAnalysis, UsageType.PremiumAnalysis),
                        plan.MonthlyAnalysisQuota),

                UsageType.PsychometricSubmission =>
                    (await CountSinceStartOfMonthAsync(userId, UsageType.PsychometricSubmission),
                        plan.MonthlyPsychometricQuota),

                _ => (0, (int?)null)
            };

            if (quota is not null && used >= quota)
            {
                _Logger.LogInformation("Quota exhausted for user {UserId} on {UsageType} ({Used}/{Quota}).", userId, usageType, used, quota);

                throw new QuotaExceededException(
                    $"You have used all {quota} included {UsageLabel(usageType)} for this month. Upgrade your plan to continue.",
                    UpgradeHint);
            }
        }

        public async Task<bool> HasPremiumEntitlementAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            var plan = await ResolveEffectivePlanAsync(userId);

            return plan.IncludesPremiumAnalysis;
        }

        public async Task EnsurePremiumEntitlementAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var plan = await ResolveEffectivePlanAsync(userId);

            if (!plan.IncludesPremiumAnalysis)
            {
                throw new QuotaExceededException(
                    "The Career Interest Assessment and combined reports are part of the Premium plan.",
                    UpgradeHint);
            }
        }

        public async Task RecordUsageAsync(string userId, UsageType usageType, int units = 1)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            await _Unit.UsageTransaction.AddAsync(new UsageTransaction
            {
                UserId = userId,
                UsageType = usageType,
                Units = units,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _Unit.SaveChangesAsync();
        }

        public async Task<UsageSummaryDto> GetUsageSummaryAsync(string userId)
        {
            var subscription = await _Unit.UserSubscription.GetActiveForUserAsync(userId);
            var plan = subscription?.Plan;

            return new UsageSummaryDto
            {
                PlanCode = plan?.Code ?? "free",
                PlanName = plan?.Name ?? "Free",
                AnalysesUsedThisMonth = await CountSinceStartOfMonthAsync(userId, UsageType.AcademicAnalysis, UsageType.PremiumAnalysis),
                // A null quota means unlimited — only fall back to the free allowance when there
                // is no subscription at all (plan is null), not when the plan allows unlimited use.
                MonthlyAnalysisQuota = plan is not null ? plan.MonthlyAnalysisQuota : FreeMonthlyAnalyses,
                PsychometricSubmissionsThisMonth = await CountSinceStartOfMonthAsync(userId, UsageType.PsychometricSubmission),
                MonthlyPsychometricQuota = plan is not null ? plan.MonthlyPsychometricQuota : FreeMonthlyPsychometricSubmissions,
                PremiumAnalysisUnlocked = plan is not null && plan.IncludesPremiumAnalysis,
                PsychometricsUnlocked = plan is not null && plan.IncludesPremiumAnalysis,
                CreditBalance = await _Unit.CreditTransaction.GetBalanceAsync(userId),
                CurrentPeriodEndUtc = subscription?.CurrentPeriodEndUtc
            };
        }

        public async Task<CheckoutResponseDto> InitiateCheckoutAsync(string userId, string planCode)
        {
            var plan = await _Unit.Plan.GetByCodeAsync(planCode)
                       ?? throw new PlanNotFoundException($"No plan exists with code '{planCode}'.");

            if (!plan.IsActive)
            {
                throw new PlanNotFoundException($"Plan '{planCode}' is not currently available.");
            }

            var user = await _Unit.User.GetByUserIdAsync(userId)
                       ?? throw new KeyNotFoundException("No account exists for the logged-in user.");

            if (!_Gateway.IsConfigured)
            {
                return new CheckoutResponseDto
                {
                    Success = false,
                    Message = "Online payments are launching soon. You'll be notified when checkout opens."
                };
            }

            var providerPlanCode = ResolveProviderPlanCode(plan);

            if (plan.Interval != PlanInterval.OneOff && string.IsNullOrWhiteSpace(providerPlanCode))
            {
                // The gateway can still charge the amount once; it just cannot auto-renew.
                // Entitlement is still granted for a full period on success.
                _Logger.LogWarning(
                    "Plan {PlanCode} is recurring but has no provider plan code; charging once without auto-renew.",
                    plan.Code);
            }

            var transaction = new PaymentTransaction
            {
                PaymentTransactionId = Guid.NewGuid(),
                UserId = userId,
                PlanId = plan.PlanId,
                Purpose = plan.Interval == PlanInterval.OneOff ? PaymentPurpose.OnceOffPurchase : PaymentPurpose.Subscription,
                AmountInCents = plan.PriceInCents,
                Currency = plan.Currency,
                Reference = NewReference()
            };

            await _Unit.PaymentTransaction.AddAsync(transaction);
            await _Unit.SaveChangesAsync();

            var result = await _Gateway.InitializeTransactionAsync(transaction, user.Email!, providerPlanCode);

            if (!result.Success)
            {
                transaction.Status = PaymentStatus.Failed;
                transaction.FailureReason = result.ErrorMessage;
                _Unit.PaymentTransaction.Update(transaction);
                await _Unit.SaveChangesAsync();

                return new CheckoutResponseDto { Success = false, Message = result.ErrorMessage };
            }

            transaction.ProviderTransactionRef = result.ProviderTransactionRef;
            _Unit.PaymentTransaction.Update(transaction);
            await _Unit.SaveChangesAsync();

            return new CheckoutResponseDto
            {
                Success = true,
                AuthorizationUrl = result.AuthorizationUrl,
                Reference = transaction.Reference,
                PlanCode = plan.Code,
                PlanName = plan.Name
            };
        }

        /// <summary>
        /// Confirms a checkout directly with the provider and grants the plan. Used by the SPA
        /// callback so an upgrade is reflected immediately even if the webhook is delayed or
        /// cannot reach a local API. Idempotent: a transaction already marked successful is a no-op.
        /// </summary>
        public async Task<CheckoutResponseDto> VerifyCheckoutAsync(string userId, string reference)
        {
            var transaction = await _Unit.PaymentTransaction.GetByReferenceAsync(reference);

            if (transaction is null || transaction.UserId != userId)
            {
                return new CheckoutResponseDto { Success = false, Message = "That payment reference is not associated with your account." };
            }

            var plan = transaction.Plan
                       ?? (transaction.PlanId is not null
                           ? await _Unit.Plan.GetByIdAsync(transaction.PlanId.Value)
                           : null);

            if (transaction.Status != PaymentStatus.Success)
            {
                var verification = await _Gateway.VerifyTransactionAsync(reference);

                if (verification is null || !verification.Paid)
                {
                    return new CheckoutResponseDto
                    {
                        Success = false,
                        Reference = reference,
                        PlanCode = plan?.Code,
                        PlanName = plan?.Name,
                        Message = "We could not confirm that payment yet. If you were charged, it will activate shortly."
                    };
                }

                await MarkPaymentSucceededAsync(reference, verification.ProviderTransactionRef);
            }

            return new CheckoutResponseDto
            {
                Success = true,
                Reference = reference,
                PlanCode = plan?.Code,
                PlanName = plan?.Name
            };
        }

        /// <summary>
        /// Development-only: activates a plan without a payment provider so the three tiers can
        /// be exercised locally. Disabled unless the app runs in Development AND
        /// Billing:AllowDevActivation is true.
        /// </summary>
        public async Task<CheckoutResponseDto> ActivatePlanForDevelopmentAsync(string userId, string planCode)
        {
            if (!DevActivationEnabled)
            {
                throw new PlanNotFoundException("Direct plan activation is not available in this environment.");
            }

            var plan = await _Unit.Plan.GetByCodeAsync(planCode)
                       ?? throw new PlanNotFoundException($"No plan exists with code '{planCode}'.");

            if (!plan.IsActive)
            {
                throw new PlanNotFoundException($"Plan '{planCode}' is not currently available.");
            }

            if (plan.Interval == PlanInterval.OneOff && plan.PriceInCents > 0)
            {
                // Free is a one-off R0 plan and is a no-op; other one-offs are real purchases.
                _Logger.LogInformation("Dev activation of one-off plan {PlanCode} for user {UserId}.", plan.Code, userId);
            }

            await GrantPlanAsync(userId, plan, providerRef: null);

            await _Unit.SaveChangesAsync();

            _Logger.LogInformation("DEVELOPMENT: plan {PlanCode} activated for user {UserId} without payment.",
                plan.Code, userId);

            return new CheckoutResponseDto
            {
                Success = true,
                PlanCode = plan.Code,
                PlanName = plan.Name,
                Message = "Plan activated for development testing."
            };
        }

        /// <summary>The provider's recurring-plan code, if this catalogue entry has one configured.</summary>
        private string? ResolveProviderPlanCode(Plan plan)
        {
            if (!string.IsNullOrWhiteSpace(plan.ProviderPlanCode))
            {
                return plan.ProviderPlanCode;
            }

            return _Paystack.PlanCodes.TryGetValue(plan.Code, out var code) && !string.IsNullOrWhiteSpace(code)
                ? code
                : null;
        }

        public async Task<IEnumerable<PlanDto>> GetActivePlansAsync()
        {
            var plans = await _Unit.Plan.GetActivePlansAsync();

            return plans.Select(p => new PlanDto
            {
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                Audience = p.Audience,
                Interval = p.Interval,
                PriceInCents = p.PriceInCents,
                Currency = p.Currency,
                IncludesPremiumAnalysis = p.IncludesPremiumAnalysis
            });
        }

        public async Task<bool> MarkPaymentSucceededAsync(string reference, string? providerTransactionRef)
        {
            var transaction = await _Unit.PaymentTransaction.GetByReferenceAsync(reference);

            if (transaction is null)
            {
                _Logger.LogWarning("Webhook received for unknown payment reference {Reference}.", reference);
                return false;
            }

            if (transaction.Status == PaymentStatus.Success)
            {
                return true;
            }

            transaction.Status = PaymentStatus.Success;
            transaction.ProviderTransactionRef = providerTransactionRef ?? transaction.ProviderTransactionRef;
            transaction.CompletedAtUtc = DateTime.UtcNow;
            _Unit.PaymentTransaction.Update(transaction);

            if (transaction.PlanId is not null)
            {
                await ActivateEntitlementAsync(transaction);
            }

            if (transaction.Purpose == PaymentPurpose.Credits)
            {
                await _Unit.CreditTransaction.AddAsync(new CreditTransaction
                {
                    UserId = transaction.UserId,
                    Delta = 1,
                    Reason = CreditReason.Purchase,
                    ReferenceId = transaction.PaymentTransactionId,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await _Unit.SaveChangesAsync();

            _Logger.LogInformation("Payment {Reference} succeeded — {Purpose} granted to user {UserId}.",
                reference, transaction.Purpose, transaction.UserId);

            return true;
        }

        /// <summary>Gives the user whatever the successful payment bought.</summary>
        private async Task ActivateEntitlementAsync(PaymentTransaction transaction)
        {
            var plan = transaction.Plan
                       ?? await _Unit.Plan.GetByIdAsync(transaction.PlanId!.Value);

            if (plan is null)
            {
                return;
            }

            await GrantPlanAsync(transaction.UserId, plan, transaction.ProviderTransactionRef);
        }

        /// <summary>
        /// Replaces any active subscription with a fresh period for <paramref name="plan"/>.
        /// Free is represented by the absence of an active subscription, so activating it simply
        /// cancels the current paid one.
        /// </summary>
        private async Task GrantPlanAsync(string userId, Plan plan, string? providerRef)
        {
            var existing = await _Unit.UserSubscription.GetActiveForUserAsync(userId);

            if (existing is not null)
            {
                existing.Status = SubscriptionStatus.Cancelled;
                _Unit.UserSubscription.Update(existing);
            }

            if (string.Equals(plan.Code, "free", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var now = DateTime.UtcNow;

            var periodEnd = plan.Interval switch
            {
                PlanInterval.Monthly => now.AddMonths(1),
                PlanInterval.Annually => now.AddYears(1),
                _ => now.AddDays(30)
            };

            await _Unit.UserSubscription.AddAsync(new UserSubscription
            {
                UserSubscriptionId = Guid.NewGuid(),
                UserId = userId,
                PlanId = plan.PlanId,
                Status = SubscriptionStatus.Active,
                StartedAtUtc = now,
                CurrentPeriodEndUtc = periodEnd,
                AutoRenew = plan.Interval != PlanInterval.OneOff,
                ProviderSubscriptionRef = providerRef
            });
        }

        private async Task<Plan> ResolveEffectivePlanAsync(string userId)
        {
            var subscription = await _Unit.UserSubscription.GetActiveForUserAsync(userId);

            return subscription?.Plan ?? FreePlan();
        }

        private static Plan FreePlan() => new()
        {
            Code = "free",
            Name = "Free",
            Audience = PlanAudience.Individual,
            Interval = PlanInterval.OneOff,
            MonthlyAnalysisQuota = FreeMonthlyAnalyses,
            MonthlyPsychometricQuota = FreeMonthlyPsychometricSubmissions,
            IncludesPremiumAnalysis = false
        };

        private static async Task<int> CountSinceStartOfMonthAsync(IUnitOfWork unit, string userId, params UsageType[] usageTypes)
        {
            return await unit.UsageTransaction.CountUnitsSinceAsync(userId, usageTypes, StartOfCurrentMonthUtc());
        }

        private async Task<int> CountSinceStartOfMonthAsync(string userId, params UsageType[] usageTypes)
        {
            return await CountSinceStartOfMonthAsync(_Unit, userId, usageTypes);
        }

        private static DateTime StartOfCurrentMonthUtc()
        {
            var now = DateTime.UtcNow;

            return new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        private static string UsageLabel(UsageType usageType) => usageType switch
        {
            UsageType.PsychometricSubmission => "psychometric submissions",
            UsageType.AiChatMessage => "AI messages",
            _ => "career analyses"
        };

        /// <summary>Human-friendly unique payment reference: PATHLY-yyMMdd-hhhmmssff-xxxx.</summary>
        private static string NewReference()
        {
            var random = RandomNumberGenerator.GetInt32(0x10000).ToString("X4");

            return $"PATHLY-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{random}";
        }
    }
}
