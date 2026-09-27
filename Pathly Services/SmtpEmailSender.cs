using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pathly_Core;
using Pathly_Interfaces.IService;

namespace Pathly_Services
{
    /// <summary>
    /// SMTP email sender. If SMTP isn't configured (no Host/FromAddress), it logs the message
    /// instead of throwing — so password-reset and confirmation flows remain testable locally
    /// without real credentials, and a misconfigured production inbox degrades gracefully.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<SmtpSettings> settings, ILogger<SmtpEmailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return;
            }

            if (!_settings.IsConfigured)
            {
                // Never log the body (it can contain a reset token) — just enough to confirm the flow ran.
                _logger.LogWarning(
                    "Email delivery is not configured; an email to {Recipient} with subject '{Subject}' was not sent.",
                    toEmail, subject);
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress!, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = string.IsNullOrWhiteSpace(_settings.UserName)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(_settings.UserName, _settings.Password)
            };

            try
            {
                await client.SendMailAsync(message);
                _logger.LogInformation("Sent '{Subject}' email to {Recipient}.", subject, toEmail);
            }
            catch (Exception ex)
            {
                // A failed send must not crash the auth flow (and must not reveal whether the
                // address exists). Log and move on.
                _logger.LogError(ex, "Failed to send '{Subject}' email to {Recipient}.", subject, toEmail);
            }
        }
    }
}
