using Pathly_DTOs;

namespace Pathly_Interfaces.IService
{
    public interface IAuthServiceInterface
    {
        Task<ResponseUserDto> AddNewUserAsync(UserDto dto);

        Task<ResponseUserDto> AuthenticateTheUserAsync(LoginDto dto);

        /// <summary>Sends a password-reset email if the address exists; always completes silently otherwise.</summary>
        Task RequestPasswordResetAsync(ForgotPasswordDto dto);

        /// <summary>Resets the password using an Identity reset token. Throws on an invalid/expired token.</summary>
        Task ResetPasswordAsync(ResetPasswordDto dto);

        /// <summary>Confirms an email address using an Identity confirmation token.</summary>
        Task ConfirmEmailAsync(ConfirmEmailDto dto);

        /// <summary>Re-sends the confirmation email if the address exists and is not yet confirmed.</summary>
        Task ResendConfirmationAsync(ForgotPasswordDto dto);
    }
}
