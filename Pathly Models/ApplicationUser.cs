using Microsoft.AspNetCore.Identity;
using Pathly_Enums;

namespace Pathly_Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        public string? ProfilePictures { get; set; }  

        public AuthProviders AuthProvider { get; set; }

        public string? GoogleId { get; set; }

        public string? MicrosoftId { get; set; }

        public string? Subscription { get; set; }

        public DateTime? CreatedAt { get; set; }

        /// <summary>POPIA: when the account holder accepted the Terms of Service and Privacy Policy.</summary>
        public DateTime? TermsAcceptedAtUtc { get; set; }

        /// <summary>POPIA: version of the Terms/Privacy documents the account holder accepted.</summary>
        public string? TermsVersion { get; set; }

        /// <summary>POPIA: explicit opt-in for marketing communications (never implied by registration).</summary>
        public bool MarketingConsent { get; set; }
    }
}
