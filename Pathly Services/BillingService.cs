using Microsoft.Extensions.Logging;
using Pathly_Core.Unit;
using Pathly_DTOs;
using Pathly_Enums;
using Pathly_Models;
using PathlyInterfaces.IService;
using PathlyInterfaces;
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
        private const int FreeMonthlyPsychometricSubmissions = 2;
        private const string UpgradeHint = "pro_monthly";

        private readonly IUnitOfWork _Unit;
        private readonly IPaymentGatewayInterface _Gateway;
        private readonly ILogger<BillingService> _Logger;

        public BillingService(IUnitOfWork unit,
                              IPaymentGatewayInterface gateway,
                              ILogger<BillingService> logger)
        {
            _Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _Gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task EnsureWithinQuotaAsync(string userId, UsageType usageType)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var plan = await ResolveEffectivePlanAsync(userId);

            if (usageType == UsageType.PremiumAnalysis && !plan.IncludesPremiumAnalysis)
            {
                throw new QuotaExceededException(
                    "The combined academic + psychometric analysis is part of a paid plan.",
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
                MonthlyAnalysisQuota = plan?.MonthlyAnalysisQuota ?? FreeMonthlyAnalyses,
                PsychometricSubmissionsThisMonth = await CountSinceStartOfMonthAsync(userId, UsageType.PsychometricSubmission),
                MonthlyPsychometricQuota = plan?.MonthlyPsychometricQuota ?? FreeMonthlyPsychometricSubmissions,
                PremiumAnalysisUnlocked = plan is not null && plan.IncludesPremiumAnalysis,
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

            if (plan.Interval != PlanInterval.OneOff && string.IsNullOrWhiteSpace(plan.ProviderPlanCode))
            {
                _Logger.LogError("Plan {PlanCode} is recurring but has no ProviderPlanCode configured.", planCode);

                return new CheckoutResponseDto { Success = false, Message = "This plan cannot be purchased right now." };
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

            var result = await _Gateway.InitializeTransactionAsync(transaction, user.Email!, plan.ProviderPlanCode);

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
                Reference = transaction.Reference
            };
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

            var existing = await _Unit.UserSubscription.GetActiveForUserAsync(transaction.UserId);

            if (existing is not null)
            {
                existing.Status = SubscriptionStatus.Cancelled;
                _Unit.UserSubscription.Update(existing);
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
                UserId = transaction.UserId,
                PlanId = plan.PlanId,
                Status = SubscriptionStatus.Active,
                StartedAtUtc = now,
                CurrentPeriodEndUtc = periodEnd,
                AutoRenew = plan.Interval != PlanInterval.OneOff,
                ProviderSubscriptionRef = transaction.ProviderTransactionRef
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
