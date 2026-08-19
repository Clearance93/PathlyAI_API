using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pathly_DTOs;
using Pathly_Helper;
using PathlyInterfaces.IService;

namespace Pathly_Services
{
    /// <summary>
    /// Cost-aware failover wrapper: always tries Groq first (cheapest), and only falls back to
    /// the Azure Model Router if Groq fails outright or comes back with an unusable response.
    /// Registered as the app's <see cref="IGroqService"/> so callers don't need to know about
    /// the fallback at all.
    ///
    /// Depends on <see cref="IPrimaryCareerAiProvider"/>/<see cref="IFallbackCareerAiProvider"/>
    /// rather than concrete classes so this can be unit-tested with fakes (Part 16).
    /// </summary>
    public class ResilientCareerAiService : IGroqService
    {
        private readonly IPrimaryCareerAiProvider _Primary;
        private readonly IFallbackCareerAiProvider _Fallback;
        private readonly ILogger<ResilientCareerAiService> _Logger;

        public ResilientCareerAiService(IPrimaryCareerAiProvider primary,
                                        IFallbackCareerAiProvider fallback,
                                        ILogger<ResilientCareerAiService>? logger = null)
        {
            _Primary = primary ?? throw new ArgumentNullException(nameof(primary));
            _Fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
            _Logger = logger ?? NullLogger<ResilientCareerAiService>.Instance;
        }

        public async Task<AiResponseDto> AnalyzeAcademicRecordAsync(ExtractedAcademicRecordDto academicRecord, ApsResultDto apsResult)
        {
            return await AnalyzeAcademicRecordAsync(academicRecord, apsResult, null, null);
        }

        public async Task<AiResponseDto> AnalyzeAcademicRecordAsync(
            ExtractedAcademicRecordDto academicRecord,
            ApsResultDto apsResult,
            List<CareerEvidenceDto>? careerEvidence,
            PsychometricProfileDto? psychometricProfile)
        {
            Exception? primaryFailure = null;

            try
            {
                var response = await _Primary.AnalyzeAcademicRecordAsync(academicRecord, apsResult, careerEvidence, psychometricProfile);

                if (IsUsableResponse(response))
                {
                    return response;
                }

                _Logger.LogWarning("Groq returned an empty/unusable response. Falling back to Azure Model Router.");
            }
            catch (Exception ex)
            {
                primaryFailure = ex;
                _Logger.LogWarning(ex, "Groq call failed ({ExceptionType}: {Message}). Falling back to Azure Model Router.", ex.GetType().Name, ex.Message);
            }
            try
            {
                var fallbackResponse = await _Fallback.AnalyzeAcademicRecordAsync(academicRecord, apsResult, careerEvidence, psychometricProfile);

                if (IsUsableResponse(fallbackResponse))
                {
                    return fallbackResponse;
                }

                throw new CareerAnalysisUnavailableException(
                    "Both Groq and Azure Model Router returned an unusable response.", primaryFailure);
            }
            catch (CareerAnalysisUnavailableException)
            {
                throw;
            }
            catch (Exception fallbackEx)
            {
                throw new CareerAnalysisUnavailableException(
                    "Both Groq and Azure Model Router failed to produce a career analysis.",
                    new AggregateException(
                        primaryFailure ?? new Exception("Groq returned an unusable response."),
                        fallbackEx));
            }
        }

        private static bool IsUsableResponse(AiResponseDto? response)
        {
            return response is not null
                && (!string.IsNullOrWhiteSpace(response.Summary) || response.ApsAnalysis is not null);
        }
    }
}
