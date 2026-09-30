using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Pathly_Core;
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
        private const string RefreshCookieName = "pathly_rt";

        private readonly IAuthServiceInterface _Auth;
        private readonly ICaptchaVerificationService _Captcha;
        private readonly AuthSettings _AuthSettings;

        public AuthenticationController(
            IAuthServiceInterface auth,
            ICaptchaVerificationService captcha,
            IOptions<AuthSettings> authSettings)
        {
            _Auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _Captcha = captcha ?? throw new ArgumentNullException(nameof(captcha));
            _AuthSettings = authSettings?.Value ?? new AuthSettings();
        }

        [HttpPost("registration")]
        public async Task<IActionResult> Registration(UserDto dto)
        {
            try
            {
                // Bot protection: reject scripted signups that don't solve the challenge. A no-op
                // when Turnstile is disabled/unconfigured, so no environment is locked out.
                var captchaOk = await _Captcha.VerifyAsync(dto.CaptchaToken, HttpContext.Connection.RemoteIpAddress?.ToString());

                if (!captchaOk)
                {
                    return BadRequest(new
                    {
                        error = "captcha_failed",
                        message = "We couldn't verify that you're human. Please complete the challenge and try again."
                    });
                }

                var newUser = await _Auth.AddNewUserAsync(dto);

                if (newUser != null)
                {
                    SetRefreshCookie(newUser.RefreshToken);
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

                SetRefreshCookie(returnUser.RefreshToken);
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
        /// Exchanges a (rotating) refresh token for a fresh access token. The token is read from
        /// the request body when supplied, otherwise from the httpOnly cookie.
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto? dto)
        {
            var token = dto?.RefreshToken;

            if (string.IsNullOrWhiteSpace(token))
            {
                Request.Cookies.TryGetValue(RefreshCookieName, out token);
            }

            try
            {
                var refreshed = await _Auth.RefreshAsync(token ?? string.Empty);

                SetRefreshCookie(refreshed.RefreshToken);
                return Ok(refreshed);
            }
            catch (InvalidCredentialsException)
            {
                ClearRefreshCookie();
                return Unauthorized(new { message = "Your session has expired. Please sign in again." });
            }
        }

        /// <summary>Revokes the supplied/cookie refresh token (sign-out).</summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto? dto)
        {
            var token = dto?.RefreshToken;

            if (string.IsNullOrWhiteSpace(token))
            {
                Request.Cookies.TryGetValue(RefreshCookieName, out token);
            }

            await _Auth.RevokeRefreshTokenAsync(token ?? string.Empty);

            ClearRefreshCookie();
            return Ok(new { message = "Signed out." });
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

        private void SetRefreshCookie(string? token)
        {
            if (!_AuthSettings.UseRefreshTokenCookie || string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            Response.Cookies.Append(RefreshCookieName, token, BuildCookieOptions());
        }

        private void ClearRefreshCookie()
        {
            Response.Cookies.Delete(RefreshCookieName, BuildCookieOptions());
        }

        private CookieOptions BuildCookieOptions() => new()
        {
            HttpOnly = true,
            // SameSite=None requires Secure; fall back to Lax on plain-HTTP local development.
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/api/Authentication",
            Expires = DateTimeOffset.UtcNow.AddDays(_AuthSettings.RefreshTokenDays > 0 ? _AuthSettings.RefreshTokenDays : 30)
        };
    }
}
