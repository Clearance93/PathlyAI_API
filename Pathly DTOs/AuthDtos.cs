namespace Pathly_DTOs
{
    /// <summary>Request a password-reset email. The response is identical whether or not the address exists.</summary>
    public class ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>Complete a password reset using the token from the emailed link.</summary>
    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>Confirm an email address using the token from the emailed link.</summary>
    public class ConfirmEmailDto
    {
        public string UserId { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;
    }
}
