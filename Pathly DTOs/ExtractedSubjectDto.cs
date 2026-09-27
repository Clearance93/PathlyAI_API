namespace Pathly_DTOs
{
    public class ExtractedSubjectDto
    {
        public Guid ExtractionSubjectId { get; set; }

        public string? SubjectName { get; set; }

        public string? RawMark { get; set; }

        public int? NumericMark { get; set; }

        public string? Symbol { get; set; }

        public string? MarkType { get; set; }

        /// <summary>1-based term/semester ordinal on the source document (e.g. 2 for "Term 2"). Null on single-snapshot records.</summary>
        public int? TermOrdinal { get; set; }

        /// <summary>Free-form term label as it appeared (e.g. "Term 2", "Semester 1", "June 2025"). Null on single-snapshot records.</summary>
        public string? TermLabel { get; set; }

        /// <summary>True when this row came from the report's Final/Promotion (cumulative) column family.</summary>
        public bool IsFinal { get; set; }

        /// <summary>Canonical subject name assigned deterministically by <see cref="Pathly_Helper.SubjectCanonicalizer"/>.</summary>
        public string? CanonicalSubjectName { get; set; }
    }
}
