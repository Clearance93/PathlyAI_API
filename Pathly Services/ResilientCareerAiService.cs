using Pathly_DTOs;
using Pathly_Services.Pathly_Services;
using PathlyInterfaces.IService;

namespace Pathly_Services
{
    /// <summary>
    /// Cost-aware failover wrapper: always tries Groq first (cheapest), and only falls back to
    /// the Azure Model Router if Groq fails outright or comes back with an unusable response.
    /// Registered as the app's <see cref="IGroqService"/> so callers don't need to know about
    /// the fallback at all.
    /// </summary>
    public class ResilientCareerAiService : IGroqService
    {
        private readonly GroqService _Primary;
        private readonly AzureModelRouterService _Fallback;

        public ResilientCareerAiService(GroqService primary, AzureModelRouterService fallback)
        {
            _Primary = primary ?? throw new ArgumentNullException(nameof(primary));
            _Fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        public async Task<AiResponseDto> AnalyzeAcademicRecordAsync(ExtractedAcademicRecordDto academicRecord, ApsResultDto apsResult)
        {
            try
            {
                var response = await _Primary.AnalyzeAcademicRecordAsync(academicRecord, apsResult);

                if (IsUsableResponse(response))
                {
                    return response;
                }

                Console.WriteLine("Groq returned an empty/unusable response. Falling back to Azure Model Router.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Groq call failed ({ex.GetType().Name}: {ex.Message}). Falling back to Azure Model Router.");
            }

            return await _Fallback.AnalyzeAcademicRecordAsync(academicRecord, apsResult);
        }

        private static bool IsUsableResponse(AiResponseDto? response)
        {
            return response is not null
                && (!string.IsNullOrWhiteSpace(response.Summary) || response.ApsAnalysis is not null);
        }
    }
}
