using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pathly_Core.Unit;
using Pathly_DTOs;
using Pathly_Interfaces.IService;

namespace Pathly_Services
{
    /// <summary>
    /// Serves a learner's persisted analysis history and individual stored reports, always
    /// scoped to the requesting account. This is what turns "generate a report" into durable,
    /// cross-device, per-user data rather than something that only lives in the browser.
    /// </summary>
    public class AnalysisQueryService : IAnalysisQueryService
    {
        private readonly IUnitOfWork _Unit;
        private readonly ILogger<AnalysisQueryService> _Logger;

        public AnalysisQueryService(IUnitOfWork unit, ILogger<AnalysisQueryService> logger)
        {
            _Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AnalysisHistoryResponseDto> GetHistoryAsync(string applicationUserId)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
            {
                throw new ArgumentException("A user id is required.", nameof(applicationUserId));
            }

            var rows = await _Unit.AiResponse.GetHistoryForUserAsync(applicationUserId);

            var items = rows.Select(r => new AnalysisHistoryItemDto
            {
                AiResponseId = r.AiResponseId,
                ExtractionAcademicRecordId = r.ExtractionAcademicRecordId,
                GeneratedAt = r.AddedAt,
                StudyLevel = r.Grade,
                DriverTermLabel = r.DriverTermLabel,
                OverallScore = r.OverallScore,
                CalculatedAps = r.ApsAnalysis?.CalculatedAps,
                IsPremium = r.IsPremium,
                AcademicPersonality = r.AcademicPersonality
            }).ToList();

            // Oldest-first trend so the UI can chart progress over time.
            var trend = items
                .Where(i => i.CalculatedAps is not null)
                .OrderBy(i => i.GeneratedAt)
                .Select(i => new ApsTrendPointDto
                {
                    GeneratedAt = i.GeneratedAt,
                    CalculatedAps = i.CalculatedAps!.Value,
                    Label = i.DriverTermLabel
                })
                .ToList();

            return new AnalysisHistoryResponseDto { Items = items, ApsTrend = trend };
        }

        public async Task<AiResponseDto?> GetResultAsync(Guid aiResponseId, string applicationUserId)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
            {
                throw new ArgumentException("A user id is required.", nameof(applicationUserId));
            }

            var row = await _Unit.AiResponse.GetByIdForUserAsync(aiResponseId, applicationUserId);

            if (row is null)
            {
                return null;
            }

            AiResponseDto? dto = null;

            if (!string.IsNullOrWhiteSpace(row.ResponseJson))
            {
                try
                {
                    dto = JsonSerializer.Deserialize<AiResponseDto>(
                        row.ResponseJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException ex)
                {
                    _Logger.LogWarning(ex, "Stored response JSON for {AiResponseId} could not be deserialized.", aiResponseId);
                }
            }

            dto ??= new AiResponseDto();

            // Re-stamp the identity/ownership metadata from the row — the serialized payload was
            // captured before these were known.
            dto.AiResponseId = row.AiResponseId;
            dto.ExtractionAcademicRecordId = row.ExtractionAcademicRecordId;
            dto.DriverTermLabel = row.DriverTermLabel;
            dto.IsPremium = row.IsPremium;
            dto.GeneratedAt = row.AddedAt;

            return dto;
        }
    }
}
