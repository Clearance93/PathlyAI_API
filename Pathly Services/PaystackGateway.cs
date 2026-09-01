using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pathly_Core;
using PathlyInterfaces.IService;
using Pathly_Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Pathly_Services
{
    /// <summary>
    /// Paystack implementation of checkout + webhook verification. Amounts are sent in
    /// subunits (cents), which is exactly how PaymentTransaction stores them. Webhook
    /// authenticity is proven with the HMAC-SHA512 signature Paystack documents.
    /// </summary>
    public class PaystackGateway : IPaymentGatewayInterface
    {
        private readonly HttpClient _HttpClient;
        private readonly PaystackSettings _Settings;
        private readonly ILogger<PaystackGateway> _Logger;

        public string ProviderName => "paystack";

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_Settings.SecretKey);

        public PaystackGateway(HttpClient httpClient,
                               IOptions<PaystackSettings> settings,
                               ILogger<PaystackGateway> logger)
        {
            _HttpClient = httpClient;
            _Settings = settings.Value;
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GatewayCheckoutResult> InitializeTransactionAsync(PaymentTransaction transaction, string payerEmail, string? providerPlanCode)
        {
            if (!IsConfigured)
            {
                return new GatewayCheckoutResult { Success = false, ErrorMessage = "Payment gateway is not configured." };
            }

            var payload = new Dictionary<string, object?>
            {
                ["email"] = payerEmail,
                ["amount"] = transaction.AmountInCents,
                ["currency"] = transaction.Currency,
                ["reference"] = transaction.Reference,
                ["metadata"] = new { purpose = transaction.Purpose.ToString(), userId = transaction.UserId }
            };

            if (!string.IsNullOrWhiteSpace(providerPlanCode))
            {
                payload["plan"] = providerPlanCode;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_Settings.BaseUrl.TrimEnd('/')}/transaction/initialize")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _Settings.SecretKey.Trim());

            var response = await _HttpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _Logger.LogWarning("Paystack initialize failed ({StatusCode}): {Body}", (int)response.StatusCode, body);
                return new GatewayCheckoutResult { Success = false, ErrorMessage = "The payment provider rejected the request." };
            }

            using var doc = JsonDocument.Parse(body);

            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("authorization_url", out var url))
            {
                string? providerRef = null;
                if (data.TryGetProperty("access_code", out var accessCode))
                {
                    providerRef = accessCode.GetString();
                }

                return new GatewayCheckoutResult
                {
                    Success = true,
                    AuthorizationUrl = url.GetString(),
                    ProviderTransactionRef = providerRef
                };
            }

            return new GatewayCheckoutResult { Success = false, ErrorMessage = "Unexpected response from the payment provider." };
        }

        public bool VerifyWebhookSignature(string rawBody, string signatureHeader)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(signatureHeader))
            {
                return false;
            }

            var expected = ComputeSignature(rawBody);

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(signatureHeader));
        }

        private string ComputeSignature(string rawBody)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_Settings.SecretKey.Trim());
            var bodyBytes = Encoding.UTF8.GetBytes(rawBody);

            using var hmac = new HMACSHA512(keyBytes);

            return Convert.ToHexString(hmac.ComputeHash(bodyBytes)).ToLowerInvariant();
        }
    }
}
