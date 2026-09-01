using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pathly_DTOs;
using Pathly_Enums;
using PathlyInterfaces.IService;

namespace PathlyAI_API.Controllers
{
    /// <summary>
    /// Storage API for psychometric assessments. The assessment UI posts every answered question
    /// plus the computed RIASEC results here, tied to the id of the logged-in user, and can pull
    /// the learner's last stored results back. Pure persistence/retrieval — no LLM involvement.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PsychometricController : ControllerBase
    {
        private readonly IPsychometricService _PsychometricService;
        private readonly IBillingServiceInterface _Billing;

        public PsychometricController(IPsychometricService psychometricService,
                                      IBillingServiceInterface billing)
        {
            _PsychometricService = psychometricService ?? throw new ArgumentNullException(nameof(psychometricService));
            _Billing = billing ?? throw new ArgumentNullException(nameof(billing));
        }

        /// <summary>Stores a completed assessment (answers + results) against the logged-in user.</summary>
        [HttpPost("assessment")]
        public async Task<IActionResult> SubmitAssessment([FromBody] PsychometricSubmissionDto submission)
        {
            var userId = User.FindFirstValue("extension_userId");

            try
            {
                await _Billing.EnsureWithinQuotaAsync(userId ?? string.Empty, UsageType.PsychometricSubmission);

                if (submission is null || string.IsNullOrWhiteSpace(submission.UserId))
                {
                    return BadRequest(new { message = "The id of the logged-in user is required to store an assessment." });
                }

                var stored = await _PsychometricService.SubmitAssessmentAsync(submission);

                await _Billing.RecordUsageAsync(userId ?? string.Empty, UsageType.PsychometricSubmission);

                return Ok(stored);
            }
            catch (QuotaExceededException ex)
            {
                return StatusCode(StatusCodes.Status402PaymentRequired, new
                {
                    error = "quota_exceeded",
                    message = ex.Message,
                    upgradePlan = ex.RequiredPlanHint
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Returns the user's most recent stored assessment and profile, if one exists.</summary>
        [HttpGet("assessment/{userId}")]
        public async Task<IActionResult> GetLatestAssessment(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new { message = "A user id is required." });
            }

            var latest = await _PsychometricService.GetLatestForUserAsync(userId);

            if (latest is null)
            {
                return NotFound(new { message = $"No stored psychometric assessment exists for user '{userId}' yet." });
            }

            return Ok(latest);
        }
    }
}
