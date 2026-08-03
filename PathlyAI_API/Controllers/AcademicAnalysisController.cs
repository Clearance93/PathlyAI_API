using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pathly_DTOs;
using PathlyInterfaces.IService;

namespace PathlyAI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AcademicAnalysisController : ControllerBase
    {
        private readonly ICareerAnalysisService _CareerService;

        public AcademicAnalysisController(ICareerAnalysisService careerService)
        {
            _CareerService = careerService ?? throw new ArgumentNullException(nameof(careerService));
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

            var result = await _CareerService.AnalyzeAsync(dto.Base64File, dto.MimeType, dto.FileName);
        
            return Ok(result);
        }
    }
}
