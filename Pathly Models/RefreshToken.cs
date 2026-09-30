using System.ComponentModel.DataAnnotations;

namespace Pathly_Models
{
    /// <summary>
    /// A long-lived, rotating refresh token bound to an account. Only the SHA-256 hash of the raw
    /// token is stored, so a database leak cannot be replayed. Rotation on every use (plus reuse
    /// detection) lets Pathly revoke a whole session chain if a token is replayed.
    /// </summary>
    public class RefreshToken
    {
        [Key]
        public Guid RefreshTokenId { get; set; }

        public string ApplicationUserId { get; set; } = string.Empty;

        public ApplicationUser? ApplicationUser { get; set; }

        /// <summary>SHA-256 hash (hex) of the raw refresh token — the raw value is never stored.</summary>
        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? RevokedAtUtc { get; set; }

        /// <summary>Hash of the token that replaced this one during rotation (audit/reuse detection).</summary>
        public string? ReplacedByTokenHash { get; set; }

        public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
    }
}
