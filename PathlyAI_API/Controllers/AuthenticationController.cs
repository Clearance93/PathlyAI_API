using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pathly_DTOs;
using Pathly_Helper;
using Pathly_Interfaces.IService;

namespace PathlyAI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthServiceInterface _Auth;

        public AuthenticationController(IAuthServiceInterface auth)
        {
            _Auth = auth ?? throw new ArgumentNullException(nameof(auth));
        }

        [HttpPost("registration")]
        public async Task<IActionResult> Registration(UserDto dto)
        {
            try
            {
                var newUser = await _Auth.AddNewUserAsync(dto);

                if (newUser != null)
                {
                    return Ok(newUser);
                }

                return BadRequest(new { message = "Failed to add new user" });
            }
            catch (KeyNotFoundException)
            {
                return Conflict(new { message = "An account with this email already exists." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try
            {
                var returnUser = await _Auth.AuthenticateTheUserAsync(dto);

                return Ok(returnUser);
            }
            catch (AccountLockedException ex)
            {
                return StatusCode(StatusCodes.Status423Locked, new { message = ex.Message });
            }
            catch (EmailNotConfirmedException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    error = "email_not_confirmed",
                    message = ex.Message
                });
            }
            catch (InvalidCredentialsException)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }
        }

        /// <summary>
        /// Starts the password-reset flow. Always returns the same response whether or not the
        /// address is registered, so the endpoint cannot be used to discover accounts.
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            await _Auth.RequestPasswordResetAsync(dto);

            return Ok(new { message = "If an account exists for that email, a password reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            try
            {
                await _Auth.ResetPasswordAsync(dto);

                return Ok(new { message = "Your password has been updated. You can now sign in." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailDto dto)
        {
            try
            {
                await _Auth.ConfirmEmailAsync(dto);

                return Ok(new { message = "Your email address has been confirmed. You can now sign in." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation(ForgotPasswordDto dto)
        {
            await _Auth.ResendConfirmationAsync(dto);

            return Ok(new { message = "If your account still needs verification, a new link has been sent." });
        }
    }
}
