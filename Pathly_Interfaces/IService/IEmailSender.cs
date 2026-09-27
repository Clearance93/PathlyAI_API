namespace Pathly_Interfaces.IService
{
    /// <summary>Minimal outbound email abstraction so auth flows (reset, confirmation) don't depend on SMTP directly.</summary>
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }
}
