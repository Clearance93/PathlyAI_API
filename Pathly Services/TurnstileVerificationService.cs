using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pathly_Core;
using Pathly_Interfaces.IService;

namespace Pathly_Services
{
    /// <summary>
    /// Cloudflare Turnstile server-side verification. The frontend solves the widget and posts the
    /// resulting token here; this service checks it against Cloudflare, bound to the caller's IP.
    /// When the feature is disabled/unconfigured it returns true so no environment is ever
    /// accidentally locked out.
    /// </summary>
    public class TurnstileVerificationService : ICaptchaVerificationService
    {
        private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        private readonly HttpClient _http;
        private readonly TurnstileSettings _settings;
        private readonly ILogger<TurnstileVerificationService> _logger;

        public TurnstileVerificationService(
            HttpClient http,
            IOptions<TurnstileSettings> settings,
            ILogger<TurnstileVerificationService> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _settings = settings?.Value ?? new TurnstileSettings();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> VerifyAsync(string? token, string? remoteIp)
        {
            if (!_settings.IsConfigured)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Turnstile verification failed: no token was supplied.");
                return false;
            }

            try
            {
                var fields = new List<KeyValuePair<string, string>>
                {
                    new("secret", _settings.SecretKey!),
                    new("response", token)
                };

                if (!string.IsNullOrWhiteSpace(remoteIp))
                {
                    fields.Add(new("remoteip", remoteIp));
                }

                using var content = new FormUrlEncodedContent(fields);
                using var response = await _http.PostAsync(VerifyUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Turnstile verification returned HTTP {StatusCode}.", response.StatusCode);
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<TurnstileResponse>();
                return result?.Success == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Turnstile verification call failed.");
                return false;
            }
        }

        private sealed class TurnstileResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }
        }
    }
}
