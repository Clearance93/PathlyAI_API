using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pathly_Interfaces.IService;

namespace PathlyAI_API.Controllers
{
    /// <summary>
    /// POPIA data-subject rights for the logged-in account: download everything Pathly holds
    /// about you, or delete your account and personal data.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _Account;

        public AccountController(IAccountService account)
        {
            _Account = account ?? throw new ArgumentNullException(nameof(account));
        }

        /// <summary>Exports all personal information held for the logged-in account as JSON.</summary>
        [HttpGet("data-export")]
        public async Task<IActionResult> ExportData()
        {
            var userId = User.FindFirstValue("extension_userId");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "A valid access token is required." });
            }

            var export = await _Account.ExportDataAsync(userId);

            return Ok(export);
        }

        /// <summary>Deletes the logged-in account and its personal analysis/assessment data.</summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = User.FindFirstValue("extension_userId");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "A valid access token is required." });
            }

            try
            {
                await _Account.DeleteAccountAsync(userId);

                return Ok(new { message = "Your account and personal data have been deleted." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
