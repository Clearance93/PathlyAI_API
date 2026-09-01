using Pathly_Models;

namespace PathlyInterfaces.IService
{
    /// <summary>Abstraction over the payment provider so gateways can be swapped without touching billing logic.</summary>
    public interface IPaymentGatewayInterface
    {
        string ProviderName { get; }

        bool IsConfigured { get; }

        /// <summary>Creates a hosted checkout session and returns the URL the browser must open.</summary>
        Task<GatewayCheckoutResult> InitializeTransactionAsync(PaymentTransaction transaction, string payerEmail, string? providerPlanCode);

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
}
