namespace Pathly_DTOs
{
    public class ResponseUserDto
    {
        public string? Token { get; set; }

        public DateTime ExpirationDate { get; set; }

        public string? Email { get; set; }

        /// <summary>ApplicationUser.Id of the logged-in user � the UI stores this and sends it
        /// back when persisting psychometric assessments against the account.</summary>
        public string? UserId { get; set; }

        public string? FullName { get; set; }

        /// <summary>
        /// Effective plan tier for this account: "free", "standard" or "premium".
        /// Derived from the active subscription's entitlements, so the SPA never has to
        /// guess a tier from a raw catalogue code.
        /// </summary>
        public string Plan { get; set; } = "free";

        /// <summary>
        /// Raw rotating refresh token. Also set as an httpOnly cookie by the API; returned here so
        /// the SPA can keep working where cross-site cookies are blocked. Never logged.
        /// </summary>
        public string? RefreshToken { get; set; }
    }
}
