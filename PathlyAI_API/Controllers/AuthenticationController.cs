using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pathly_DTOs;
using Pathly_Helper;
using PathlyInterfaces.IService;

namespace PathlyAI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
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
            catch (InvalidCredentialsException)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }
        }
    }
}
