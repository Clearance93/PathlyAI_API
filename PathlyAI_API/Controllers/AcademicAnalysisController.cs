using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pathly_DTOs;
using Pathly_Enums;
using Pathly_Helper;
using Pathly_Interfaces.IService;
using System.Security.Claims;

namespace PathlyAI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AcademicAnalysisController : ControllerBase
    {
        private const long MaxUploadBytes = 5 * 1024 * 1024;

        private readonly ICareerAnalysisService _CareerService;
        private readonly IPremiumCareerAnalysisService _PremiumCareerService;
        private readonly IBillingServiceInterface _Billing;
        private readonly IAnalysisQueryService _AnalysisQuery;

        public AcademicAnalysisController(ICareerAnalysisService careerService,
                                          IPremiumCareerAnalysisService premiumCareerService,
                                          IBillingServiceInterface billing,
                                          IAnalysisQueryService analysisQuery)
        {
            _CareerService = careerService ?? throw new ArgumentNullException(nameof(careerService));
            _PremiumCareerService = premiumCareerService ?? throw new ArgumentNullException(nameof(premiumCareerService));
            _Billing = billing ?? throw new ArgumentNullException(nameof(billing));
            _AnalysisQuery = analysisQuery ?? throw new ArgumentNullException(nameof(analysisQuery));
        }

        private static long EstimateDecodedSize(string base64)
        {
            var len = base64.Length;
            var padding = base64.EndsWith("==", StringComparison.Ordinal) ? 2
                        : base64.EndsWith("=", StringComparison.Ordinal) ? 1
                        : 0;

            return (long)(len * 0.75) - padding;
        }

        [HttpPost("analysis")]
        public async Task<IActionResult> Analyze([FromBody] AcademicRecordUploadDto dto)
        {
            var userId = User.FindFirstValue("extension_userId");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "A valid access token is required." });
            }

            try
            {
                await _Billing.EnsureWithinQuotaAsync(userId, UsageType.AcademicAnalysis);

                if (string.IsNullOrWhiteSpace(dto.Base64File))
                {
                    return BadRequest("No file content was provided.");
                }

                if (string.IsNullOrWhiteSpace(dto.MimeType) && string.IsNullOrWhiteSpace(dto.FileName))
                {
                    return BadRequest("Either MimeType or FileName must be provided.");
                }

                if (EstimateDecodedSize(dto.Base64File) > MaxUploadBytes)
                {
                    return BadRequest(new
                    {
                        error = "file_too_large",
                        message = $"The file is larger than the {MaxUploadBytes / (1024 * 1024)} MB limit. " +
                                  "Please compress or re-take the photo and try again."
                    });
                }

                var result = await _CareerService.AnalyzeAsync(dto.Base64File, dto.MimeType, dto.FileName, userId);

                await _Billing.RecordUsageAsync(userId ?? string.Empty, UsageType.AcademicAnalysis);

                return Ok(result);
            }
            catch (DocumentTextExtractionException ex)
            {
                return BadRequest(new { error = "document_not_readable", message = ex.Message });
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
                    message = "We couldn't generate a career analysis right now. Please try again shortly.",
                    detail = ex.Message
                });
            }
        }

        [HttpPost("analysis/premium")]
        public async Task<IActionResult> AnalyzePremium([FromBody] PremiumAcademicRecordUploadDto dto)
        {
            var userId = User.FindFirstValue("extension_userId");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "A valid access token is required." });
            }

            try
            {
                await _Billing.EnsureWithinQuotaAsync(userId, UsageType.PremiumAnalysis);

                if (string.IsNullOrWhiteSpace(dto.Base64File))
                {
                    return BadRequest("No file content was provided.");
                }

                if (string.IsNullOrWhiteSpace(dto.MimeType) && string.IsNullOrWhiteSpace(dto.FileName))
                {
                    return BadRequest("Either MimeType or FileName must be provided.");
                }

                if (EstimateDecodedSize(dto.Base64File) > MaxUploadBytes)
                {
                    return BadRequest(new
                    {
                        error = "file_too_large",
                        message = $"The file is larger than the {MaxUploadBytes / (1024 * 1024)} MB limit. " +
                                  "Please compress or re-take the photo and try again."
                    });
                }

                if (dto.PsychometricProfile is null)
                {
                    return BadRequest("A psychometric profile is required for the premium analysis.");
                }

                var result = await _PremiumCareerService.AnalyzeWithPsychometricsAsync(dto.Base64File, dto.MimeType, dto.FileName, dto.PsychometricProfile, userId);

                await _Billing.RecordUsageAsync(userId ?? string.Empty, UsageType.PremiumAnalysis);

                return Ok(result);
            }
            catch (DocumentTextExtractionException ex)
            {
                return BadRequest(new { error = "document_not_readable", message = ex.Message });
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

        [HttpPost("psychometric-analysis")]
        public async Task<IActionResult> AnalyzeWithStoredRecord([FromBody] PsychometricAnalysisRequestDto dto)
        {
            var userId = User.FindFirstValue("extension_userId");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "A valid access token is required." });
            }

            try
            {
                await _Billing.EnsureWithinQuotaAsync(userId, UsageType.PremiumAnalysis);

                if (dto is null || string.IsNullOrWhiteSpace(dto.ExtractionAcademicRecordId))
                {
                    return BadRequest(new { message = "An extractionAcademicRecordId from a previous analysis is required." });
                }

                if (dto.Psychometric is null)
                {
                    return BadRequest(new { message = "A psychometric profile is required for the combined analysis." });
                }

                var result = await _PremiumCareerService.AnalyzeExistingRecordWithPsychometricsAsync(
                    dto.ExtractionAcademicRecordId, userId, dto.Psychometric);

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

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = User.FindFirstValue("extension_userId");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "A valid access token is required." });
            }

            return Ok(await _AnalysisQuery.GetHistoryAsync(userId));
        }

        [HttpGet("{aiResponseId:guid}")]
        public async Task<IActionResult> GetResult(Guid aiResponseId)
        {
            var userId = User.FindFirstValue("extension_userId");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "A valid access token is required." });
            }

            var result = await _AnalysisQuery.GetResultAsync(aiResponseId, userId);

            if (result is null)
            {
                return NotFound(new { message = "No stored analysis exists with that id for your account." });
            }

            return Ok(result);
        }

        [HttpPost("combined-analysis")]
        public async Task<IActionResult> AnalyzeCombined()
        {
            var userId = User.FindFirstValue("extension_userId");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "A valid access token is required." });
            }

            try
            {
                await _Billing.EnsureWithinQuotaAsync(userId, UsageType.PremiumAnalysis);

                var result = await _PremiumCareerService.AnalyzeLatestStoredWithPsychometricsAsync(userId);

                await _Billing.RecordUsageAsync(userId, UsageType.PremiumAnalysis);

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
