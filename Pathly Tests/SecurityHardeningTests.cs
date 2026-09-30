using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pathly_Core;
using Pathly_Helper;
using Pathly_Services;
using Xunit;

namespace Pathly_Tests
{
    public class SecurityHardeningTests
    {
        [Theory]
        [InlineData("learner@gmail.com", false)]
        [InlineData("someone@school.co.za", false)]
        [InlineData("bot@mailinator.com", true)]
        [InlineData("bot@guerrillamail.com", true)]
        [InlineData("bot@inbox.mailinator.com", true)]
        [InlineData("not-an-email", false)]
        [InlineData(null, false)]
        public void Disposable_email_blocklist_rejects_throwaway_domains(string? email, bool expected)
        {
            Assert.Equal(expected, DisposableEmailDomains.IsDisposable(email));
        }

        [Fact]
        public async Task Turnstile_verification_passes_when_disabled()
        {
            var service = new TurnstileVerificationService(
                new HttpClient(),
                Options.Create(new TurnstileSettings { Enabled = false }),
                NullLogger<TurnstileVerificationService>.Instance);

            // No keys configured → never block (keeps dev/demo environments usable).
            Assert.True(await service.VerifyAsync(null, "127.0.0.1"));
        }

        [Fact]
        public async Task Turnstile_verification_rejects_missing_token_when_enabled()
        {
            var service = new TurnstileVerificationService(
                new HttpClient(),
                Options.Create(new TurnstileSettings { Enabled = true, SecretKey = "test-secret" }),
                NullLogger<TurnstileVerificationService>.Instance);

            // Enabled + no token → rejected without any network call.
            Assert.False(await service.VerifyAsync(null, "127.0.0.1"));
            Assert.False(await service.VerifyAsync("", "127.0.0.1"));
        }
    }
}
