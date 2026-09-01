namespace Pathly_Enums
{
    /// <summary>How a plan charges: once-off purchases vs recurring subscriptions.</summary>
    public enum PlanInterval
    {
        OneOff = 0,
        Monthly = 1,
        Annually = 2
    }

    /// <summary>The market segment a plan is sold to.</summary>
    public enum PlanAudience
    {
        Individual = 0,
        Student = 1,
        Professional = 2,
        Organization = 3
    }

    public enum SubscriptionStatus
    {
        Active = 0,
        PastDue = 1,
        Cancelled = 2,
        Expired = 3
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Success = 1,
        Failed = 2,
        Refunded = 3
    }

    /// <summary>What a payment was for.</summary>
    public enum PaymentPurpose
    {
        Subscription = 0,
        OnceOffPurchase = 1,
        Credits = 2
    }

    /// <summary>Billable actions the platform performs on a user's behalf.</summary>
    public enum UsageType
    {
        AcademicAnalysis = 0,
        PremiumAnalysis = 1,
        PsychometricSubmission = 2,
        AiChatMessage = 3
    }

    public enum CreditReason
    {
        Purchase = 0,
        Grant = 1,
        Spend = 2,
        Refund = 3,
        Expired = 4
    }
}
