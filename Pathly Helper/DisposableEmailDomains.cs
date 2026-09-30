namespace Pathly_Helper
{
    /// <summary>
    /// Blocks well-known disposable/throwaway email providers at registration. A cheap, dependency
    /// -free first line of defence against scripted accounts that never convert — pair with
    /// CAPTCHA and rate limiting rather than relying on it alone.
    /// </summary>
    public static class DisposableEmailDomains
    {
        private static readonly HashSet<string> Domains = new(StringComparer.OrdinalIgnoreCase)
        {
            "mailinator.com", "guerrillamail.com", "guerrillamailblock.com", "sharklasers.com",
            "10minutemail.com", "10minutemail.net", "tempmail.com", "temp-mail.org", "temp-mail.io",
            "throwawaymail.com", "throwaway.email", "yopmail.com", "yopmail.fr", "getnada.com",
            "nada.email", "trashmail.com", "dispostable.com", "maildrop.cc", "fakeinbox.com",
            "mailnesia.com", "spam4.me", "grr.la", "pokemail.net", "tempinbox.com", "mohmal.com",
            "emailondeck.com", "mintemail.com", "mytemp.email", "tempr.email", "emailfake.com",
            "mail.tm", "mailinator.net", "discard.email", "spamgourmet.com", "mailcatch.com",
            "inboxkitten.com", "moakt.com", "tempmailo.com"
        };

        public static bool IsDisposable(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            var atIndex = email.LastIndexOf('@');

            if (atIndex < 0 || atIndex == email.Length - 1)
            {
                return false;
            }

            var domain = email[(atIndex + 1)..].Trim().TrimEnd('.').ToLowerInvariant();

            return Domains.Contains(domain) ||
                   Domains.Any(d => domain.EndsWith("." + d, StringComparison.OrdinalIgnoreCase));
        }
    }
}
