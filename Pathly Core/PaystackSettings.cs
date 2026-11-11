namespace Pathly_Core
{
    /// <summary>
    /// Configuration for the Paystack payment gateway (South African card + EFT payments).
    /// Bind this to a "Paystack" section in appsettings.json / environment variables.
    /// </summary>
    public class PaystackSettings
    {
        public string BaseUrl { get; set; } = "https://api.paystack.co";

        /// <summary>Secret key from the Paystack dashboard. Store in user secrets / env vars only.</summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Where Paystack sends the browser after a successful payment. The SPA route verifies
        /// the payment with the API and then shows the upgraded plan.
        /// Defaults to the production SPA callback; override per environment.
        /// </summary>
        public string CallbackUrl { get; set; } = "https://pathly-ai.netlify.app/payment/callback";

        /// <summary>
        /// Optional mapping of catalogue plan codes to Paystack recurring-plan codes
        /// (e.g. "standard": "PLN_xxx"). When a recurring plan has no Paystack plan code it is
        /// charged as a single transaction and the entitlement is granted for one period, but
        /// the provider cannot auto-renew it.
        /// </summary>
        public Dictionary<string, string> PlanCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Controls billing behaviour that differs between environments.</summary>
    public class BillingSettings
    {
        /// <summary>
        /// Development-only escape hatch that lets a signed-in user activate a paid plan
        /// without going through the payment provider. It is ignored outside Development and
        /// must never be enabled in production.
        /// </summary>
        public bool AllowDevActivation { get; set; }
    }
}
