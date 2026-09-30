namespace Pathly_Core
{
    /// <summary>
    /// Cloudflare Turnstile (CAPTCHA) configuration. When <see cref="Enabled"/> is false the app
    /// skips verification entirely, so local development and demos work without keys. The secret
    /// key must never be exposed to the frontend — only the site key is.
    /// </summary>
    public class TurnstileSettings
    {
        public string? SiteKey { get; set; }

        public string? SecretKey { get; set; }

        public bool Enabled { get; set; }

        /// <summary>True only when enforcement is switched on AND a secret key is present.</summary>
        public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(SecretKey);
    }
}
