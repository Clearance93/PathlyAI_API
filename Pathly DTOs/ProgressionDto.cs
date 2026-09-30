namespace Pathly_DTOs
{
    /// <summary>A single measured mark for a subject in a given reported period.</summary>
    public class SubjectTrendPointDto
    {
        public string PeriodLabel { get; set; } = string.Empty;

        public int? AcademicYear { get; set; }

        public int? TermOrdinal { get; set; }

        public int? Mark { get; set; }
    }

    /// <summary>A subject's marks across every uploaded period, plus its overall direction.</summary>
    public class SubjectTrendDto
    {
        public string SubjectName { get; set; } = string.Empty;

        public string CanonicalName { get; set; } = string.Empty;

        public List<SubjectTrendPointDto> Points { get; set; } = new();

        /// <summary>Latest mark minus the earliest mark, when at least two marks exist.</summary>
        public int? ChangeSinceFirst { get; set; }

        /// <summary>"improving", "declining", "steady", or "insufficient-data".</summary>
        public string Direction { get; set; } = "insufficient-data";
    }

    /// <summary>
    /// Longitudinal view across EVERY upload for an account (not just the latest document), so
    /// Pathly's advice reflects the learner's progress over termly/yearly results instead of
    /// discarding everything before the most recent upload.
    /// </summary>
    public class ProgressionDto
    {
        public bool IsAvailable { get; set; }

        public int UploadCount { get; set; }

        public int PeriodCount { get; set; }

        /// <summary>Chronological period labels across all uploads.</summary>
        public List<string> PeriodLabels { get; set; } = new();

        public int? FirstAps { get; set; }

        public int? LatestAps { get; set; }

        public int? ApsChange { get; set; }

        public List<SubjectTrendDto> SubjectTrends { get; set; } = new();

        /// <summary>Human-readable summary of the progression, suitable for the report and prompt.</summary>
        public string Summary { get; set; } = string.Empty;
    }
}
