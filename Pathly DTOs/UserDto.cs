using Pathly_Enums;

namespace Pathly_DTOs
{
    public class UserDto
    {
        public string? Id { get; set; }

        public string? FullName { get; set; }

        /// <summary>Optional separate name parts. Composed into <see cref="FullName"/> when it is
        /// not supplied, so clients may send either shape.</summary>
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Password { get; set; }

        public string? ProfilePictures { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public AuthProviders AuthProvider { get; set; }

        public string? GoogleId { get; set; }

        public string? MicrosoftId { get; set; }

        public string? Subscription { get; set; }

        public DateTime? CreatedAt { get; set; }

        /// <summary>Registration-time acceptance of the Terms of Service and Privacy Policy (required).</summary>
        public bool AcceptTerms { get; set; }

        /// <summary>Registration-time opt-in to marketing communications (optional, never implied).</summary>
        public bool MarketingConsent { get; set; }

        /// <summary>Cloudflare Turnstile token solved by the client at registration (bot protection).</summary>
        public string? CaptchaToken { get; set; }
    }
}
