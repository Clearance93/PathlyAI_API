namespace Pathly_DTOs
{
    public class ExtractedAcademicRecordDto
    {
        public Guid ExtractionAcademicRecordId { get; set; }

        public string? StudentName { get; set; }

        public string? InstitutionName { get; set; }

        public string? InstitutionType { get; set; }

        public string? AcademicPeriod { get; set; }

        public string? StudyLevel { get; set; }

        public List<ExtractedSubjectDto> Subjects { get; set; } = new();

        public string? RawExtractedText { get; set; }

        public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// True when the free extraction pipeline could not fully validate its own output (e.g.
        /// zero subjects found, an out-of-range mark, or a duplicate subject) even after retrying.
        /// Callers/UI should surface this so a human can double-check the record rather than
        /// silently trusting a possibly-wrong extraction — see <c>ExtractionValidator</c> and
        /// <c>SelfValidatingDocumentStructuringService</c>.
        /// </summary>
        public bool NeedsManualReview { get; set; }

        /// <summary>Human-readable reasons behind <see cref="NeedsManualReview"/>, if any.</summary>
        public List<string> ExtractionWarnings { get; set; } = new();
    }
}
