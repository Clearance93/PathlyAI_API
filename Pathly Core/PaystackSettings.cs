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
    }
}
