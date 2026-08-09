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

        public ResilientCareerAiService(IPrimaryCareerAiProvider primary, IFallbackCareerAiProvider fallback)
        {
            _Primary = primary ?? throw new ArgumentNullException(nameof(primary));
            _Fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
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

                Console.WriteLine("Groq returned an empty/unusable response. Falling back to Azure Model Router.");
            }
            catch (Exception ex)
            {
                primaryFailure = ex;
                Console.WriteLine($"Groq call failed ({ex.GetType().Name}: {ex.Message}). Falling back to Azure Model Router.");
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
