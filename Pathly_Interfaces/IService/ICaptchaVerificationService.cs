namespace Pathly_Interfaces.IService
{
    /// <summary>
    /// Verifies a bot-protection (CAPTCHA) token against the provider. Returns true when the
    /// challenge passed, or when bot protection is disabled/unconfigured (so the app never blocks
    /// legitimate users in an environment without keys).
    /// </summary>
    public interface ICaptchaVerificationService
    {
        Task<bool> VerifyAsync(string? token, string? remoteIp);
    }
}
