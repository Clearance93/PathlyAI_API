namespace Pathly_DTOs
{
    /// <summary>
    /// One entry in a learner's stored analysis history. Deliberately light — it carries what a
    /// history list and an APS-over-time trend need, without shipping the entire report JSON.
    /// </summary>
    public class AnalysisHistoryItemDto
    {
        public Guid AiResponseId { get; set; }

        public Guid? ExtractionAcademicRecordId { get; set; }

        public DateTime GeneratedAt { get; set; }

        public string? StudyLevel { get; set; }

        public string? DriverTermLabel { get; set; }

        public double OverallScore { get; set; }

        public int? CalculatedAps { get; set; }

        public bool IsPremium { get; set; }

        public string? AcademicPersonality { get; set; }
    }

    /// <summary>The full history, newest first, plus a compact APS trend for progress reporting.</summary>
    public class AnalysisHistoryResponseDto
    {
        public List<AnalysisHistoryItemDto> Items { get; set; } = new();

        public List<ApsTrendPointDto> ApsTrend { get; set; } = new();
    }

    public class ApsTrendPointDto
    {
        public DateTime GeneratedAt { get; set; }

        public int CalculatedAps { get; set; }

        public string? Label { get; set; }
    }
}
