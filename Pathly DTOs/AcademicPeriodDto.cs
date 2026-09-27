namespace Pathly_DTOs
{
    /// <summary>
    /// One reported term/period block on an uploaded academic document. Carries its own subject
    /// rows so all-block history survives persistence while the analysis is driven by a single
    /// chosen driver block.
    /// </summary>
    public class AcademicPeriodDto
    {
        /// <summary>1-based term/semester ordinal within the academic year (e.g. 2 for "Term 2").</summary>
        public int Ordinal { get; set; }

        /// <summary>Free-form label as it appeared on the document (e.g. "Term 2", "June 2025").</summary>
        public string? Label { get; set; }

        /// <summary>True when this block is a Final/Promotion (cumulative) result rather than a mid-year term.</summary>
        public bool IsFinal { get; set; }

        public List<ExtractedSubjectDto> Subjects { get; set; } = new();
    }
}
