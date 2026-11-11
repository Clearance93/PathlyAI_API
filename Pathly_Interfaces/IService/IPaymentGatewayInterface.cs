using Pathly_Models;

namespace Pathly_Interfaces.IService
{
    /// <summary>Abstraction over the payment provider so gateways can be swapped without touching billing logic.</summary>
    public interface IPaymentGatewayInterface
    {
        string ProviderName { get; }

        bool IsConfigured { get; }

        /// <summary>Creates a hosted checkout session and returns the URL the browser must open.</summary>
        Task<GatewayCheckoutResult> InitializeTransactionAsync(PaymentTransaction transaction, string payerEmail, string? providerPlanCode);

        /// <summary>
        /// Confirms a transaction directly with the provider (used when the webhook cannot reach
        /// the API, e.g. local development or a missed delivery). Returns null when unknown.
        /// </summary>
        Task<GatewayVerificationResult?> VerifyTransactionAsync(string reference);

        /// <summary>Validates that a webhook payload genuinely came from the provider.</summary>
        bool VerifyWebhookSignature(string rawBody, string signatureHeader);
    }

    public class GatewayCheckoutResult
    {
        public bool Success { get; set; }

        public string? AuthorizationUrl { get; set; }

        public string? ProviderTransactionRef { get; set; }

        public string? ErrorMessage { get; set; }
    }

    public class GatewayVerificationResult
    {
        /// <summary>The provider reports the charge as successful and the amount matches.</summary>
        public bool Paid { get; set; }

        public string? ProviderTransactionRef { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
