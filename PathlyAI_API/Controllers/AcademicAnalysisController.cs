using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pathly_DTOs;
using Pathly_Helper;
using PathlyInterfaces.IService;

namespace PathlyAI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AcademicAnalysisController : ControllerBase
    {
        private readonly ICareerAnalysisService _CareerService;
        private readonly IPremiumCareerAnalysisService _PremiumCareerService;

        public AcademicAnalysisController(ICareerAnalysisService careerService,
                                          IPremiumCareerAnalysisService premiumCareerService)
        {
            _CareerService = careerService ?? throw new ArgumentNullException(nameof(careerService));
            _PremiumCareerService = premiumCareerService ?? throw new ArgumentNullException(nameof(premiumCareerService));
        }

        [HttpPost("analysis")] 
        public async Task<IActionResult> Analyze([FromBody] AcademicRecordUploadDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Base64File))
            {
                return BadRequest("No file content was provided.");
            }

            if (string.IsNullOrWhiteSpace(dto.MimeType) && string.IsNullOrWhiteSpace(dto.FileName))
            {
                return BadRequest("Either MimeType or FileName must be provided.");
            }

            try
            {
                var result = await _CareerService.AnalyzeAsync(dto.Base64File, dto.MimeType, dto.FileName);

                return Ok(result);
            }
            catch (CareerAnalysisUnavailableException ex)
            {
                // Both Groq and Azure Model Router failed — a controlled failure, not a 500 (Part 4, Step 5).
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

            try
            {
                var result = await _PremiumCareerService.AnalyzeWithPsychometricsAsync(dto.Base64File, dto.MimeType, dto.FileName, dto.PsychometricProfile);

                return Ok(result);
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
    }
}
