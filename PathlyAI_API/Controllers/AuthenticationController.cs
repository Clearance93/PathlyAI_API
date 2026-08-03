using Microsoft.AspNetCore.Mvc;
using Pathly_DTOs;
using PathlyInterfaces.IService;

namespace PathlyAI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            var newUser = await _Auth.AddNewUserAsync(dto);

            if (newUser != null)
            {
                return Ok(new
                {
                    message = newUser
                });
            }

            return BadRequest(new { message = "Failed to add new user" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var returnUser = await _Auth.AuthenticateTheUserAsync(dto);

            if (returnUser == null)
            {
                return BadRequest(new
                {
                    message = $"Failed to authenticate the user with the email: {dto.Email}"
                });
            }

            return Ok(returnUser);
        }
    }
}