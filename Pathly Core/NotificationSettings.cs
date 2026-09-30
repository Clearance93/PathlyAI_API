namespace Pathly_Core
{
    /// <summary>
    /// Outbound email configuration (SMTP). When Host/FromAddress are empty the app falls back
    /// to logging-only delivery, which keeps local development and demos working without secrets.
    /// </summary>
    public class SmtpSettings
    {
        public string? Host { get; set; }

        public int Port { get; set; } = 587;

        public string? UserName { get; set; }

        public string? Password { get; set; }

        public string? FromAddress { get; set; }

        public string? FromName { get; set; } = "PathlyAI";

        public bool EnableSsl { get; set; } = true;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
    }

    /// <summary>Public app/legal settings: the frontend base URL used in emailed links, and the current legal doc version.</summary>
    public class AppSettings
    {
        public string FrontendBaseUrl { get; set; } = "http://localhost:4200";

        public string TermsVersion { get; set; } = "2026-01";
    }

    /// <summary>Authentication policy toggles.</summary>
    public class AuthSettings
    {
        /// <summary>When true, a learner must confirm their email address before they can sign in.</summary>
        public bool RequireConfirmedEmail { get; set; }

        /// <summary>Lifetime of an issued access (JWT) token, in minutes. Short by design so a
        /// stolen token is useful for a limited window; the refresh token renews sessions.</summary>
        public int AccessTokenMinutes { get; set; } = 60;

        /// <summary>Lifetime of a refresh token, in days. Rotated on every use.</summary>
        public int RefreshTokenDays { get; set; } = 30;

        /// <summary>When true, refresh tokens are also written as an httpOnly cookie (the browser
        /// never exposes them to JavaScript). The raw token is still returned in the body so the
        /// SPA works even where cross-site cookies are blocked.</summary>
        public bool UseRefreshTokenCookie { get; set; } = true;
    }
}
