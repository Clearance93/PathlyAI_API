using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pathly_DTOs;
using Pathly_Enums;
using Pathly_Helper;
using PathlyInterfaces.IService;

namespace PathlyAI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AcademicAnalysisController : ControllerBase
    {
        private readonly ICareerAnalysisService _CareerService;
        private readonly IPremiumCareerAnalysisService _PremiumCareerService;
        private readonly IBillingServiceInterface _Billing;

        public AcademicAnalysisController(ICareerAnalysisService careerService,
                                          IPremiumCareerAnalysisService premiumCareerService,
                                          IBillingServiceInterface billing)
        {
            _CareerService = careerService ?? throw new ArgumentNullException(nameof(careerService));
            _PremiumCareerService = premiumCareerService ?? throw new ArgumentNullException(nameof(premiumCareerService));
            _Billing = billing ?? throw new ArgumentNullException(nameof(billing));
        }

        [HttpPost("analysis")]
        public async Task<IActionResult> Analyze([FromBody] AcademicRecordUploadDto dto)
        {
            var userId = User.FindFirstValue("extension_userId");

            try
            {
                await _Billing.EnsureWithinQuotaAsync(userId ?? string.Empty, UsageType.AcademicAnalysis);

                if (string.IsNullOrWhiteSpace(dto.Base64File))
                {
                    return BadRequest("No file content was provided.");
                }

                if (string.IsNullOrWhiteSpace(dto.MimeType) && string.IsNullOrWhiteSpace(dto.FileName))
                {
                    return BadRequest("Either MimeType or FileName must be provided.");
                }

                var result = await _CareerService.AnalyzeAsync(dto.Base64File, dto.MimeType, dto.FileName);

                await _Billing.RecordUsageAsync(userId ?? string.Empty, UsageType.AcademicAnalysis);

                return Ok(result);
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
            catch (CareerAnalysisUnavailableException ex)
            {
                // Both providers failed — a controlled failure, not a 500.
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    error = "career_analysis_unavailable",
                    message = "We couldn't generate a career analysis right now. Please try again shortly.",
                    detail = ex.Message
                });
            }
        }

        /// <summary>Layer 2 (Part 6/7) — premium academic + psychometric career intelligence.</summary>
        [HttpPost("analysis/premium")]
        public async Task<IActionResult> AnalyzePremium([FromBody] PremiumAcademicRecordUploadDto dto)
        {
            var userId = User.FindFirstValue("extension_userId");

            try
            {
                await _Billing.EnsureWithinQuotaAsync(userId ?? string.Empty, UsageType.PremiumAnalysis);

                if (string.IsNullOrWhiteSpace(dto.Base64File))
                {
                    return BadRequest("No file content was provided.");
                }

                if (string.IsNullOrWhiteSpace(dto.MimeType) && string.IsNullOrWhiteSpace(dto.FileName))
                {
                    return BadRequest("Either MimeType or FileName must be provided.");
                }

                if (dto.PsychometricProfile is null)
                {
                    return BadRequest("A psychometric profile is required for the premium analysis.");
                }

                var result = await _PremiumCareerService.AnalyzeWithPsychometricsAsync(dto.Base64File, dto.MimeType, dto.FileName, dto.PsychometricProfile);

                await _Billing.RecordUsageAsync(userId ?? string.Empty, UsageType.PremiumAnalysis);

                return Ok(result);
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
            catch (CareerAnalysisUnavailableException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    error = "career_analysis_unavailable",
                    message = "We couldn't generate a premium career analysis right now. Please try again shortly.",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// Combined academic + psychometric analysis reusing an ALREADY-extracted academic record
        /// (from a prior Layer 1 upload), so the learner doesn't have to re-upload their results.
        /// Cached on the exact academic + psychometric fingerprint — identical inputs are served
        /// from storage without another LLM call.
        /// </summary>
        [HttpPost("psychometric-analysis")]
        public async Task<IActionResult> AnalyzeWithStoredRecord([FromBody] PsychometricAnalysisRequestDto dto)
        {
            var userId = User.FindFirstValue("extension_userId");

            try
            {
                await _Billing.EnsureWithinQuotaAsync(userId ?? string.Empty, UsageType.PremiumAnalysis);

                if (dto is null || string.IsNullOrWhiteSpace(dto.ExtractionAcademicRecordId))
                {
                    return BadRequest(new { message = "An extractionAcademicRecordId from a previous analysis is required." });
                }

                if (dto.Psychometric is null)
                {
                    return BadRequest(new { message = "A psychometric profile is required for the combined analysis." });
                }

                var result = await _PremiumCareerService.AnalyzeExistingRecordWithPsychometricsAsync(
                    dto.ExtractionAcademicRecordId, dto.UserId, dto.Psychometric);

                await _Billing.RecordUsageAsync(userId ?? string.Empty, UsageType.PremiumAnalysis);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
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
            catch (CareerAnalysisUnavailableException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    error = "career_analysis_unavailable",
                    message = "We couldn't generate a combined career analysis right now. Please try again shortly.",
                    detail = ex.Message
                });
            }
        }
    }
}
