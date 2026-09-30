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

        /// <summary>The subject rows that DRIVE the current analysis (see driver-term selection).</summary>
        public List<ExtractedSubjectDto> Subjects { get; set; } = new();

        /// <summary>
        /// Full per-term history from the uploaded document � all populated term blocks, each
        /// term's subjects carrying their TermOrdinal/TermLabel/IsFinal. The driver block is
        /// always the highest-priority populated block: a Final/Promotion column family wins,
        /// otherwise the highest populated term ordinal. Legacy rows (pre multi-term) expose the
        /// driver block here too (IsFinal = true) so history stays lossless.
        /// </summary>
        public List<AcademicPeriodDto> AcademicPeriods { get; set; } = new();

        public string? RawExtractedText { get; set; }

        public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

        /// <summary>True when the free extraction pipeline could not fully validate its own output (e.g.
        /// zero subjects found, an out-of-range mark, or a duplicate subject) even after retrying.
        /// Callers/UI should surface this so a human can double-check the record rather than
        /// silently trusting a possibly-wrong extraction � see <c>ExtractionValidator</c> and
        /// <c>SelfValidatingDocumentStructuringService</c>.</summary>
        public bool NeedsManualReview { get; set; }

        /// <summary>Human-readable reasons behind <see cref="NeedsManualReview"/>, if any.</summary>
        public List<string> ExtractionWarnings { get; set; } = new();

        /// <summary>Id of the ApplicationUser whose account uploaded this record. Null for legacy rows.</summary>
        public string? ApplicationUserId { get; set; }

        /// <summary>School/portal learner number, when present on the document (e.g. "422215381").</summary>
        public string? LearnerNo { get; set; }

        /// <summary>School admission number, when present on the document (e.g. "023169").</summary>
        public string? AdmissionNo { get; set; }

        /// <summary>Academic year this document covers (e.g. 2026) when it can be determined.</summary>
        public int? AcademicYear { get; set; }

        /// <summary>Term ordinal of the block that drove this analysis. Null for legacy rows.</summary>
        public int? DriverTermOrdinal { get; set; }

        /// <summary>Term label of the block that drove this analysis. Null for legacy rows.</summary>
        public string? DriverTermLabel { get; set; }

        /// <summary>True when the driver block is a Final/Promotion (cumulative) column. Legacy rows default to true.</summary>
        public bool DriverIsFinal { get; set; }

        /// <summary>
        /// Deterministic next-term projection computed from this record's term history, passed to
        /// the analysis prompt as evidence. Null when the record does not qualify for prediction.
        /// </summary>
        public AcademicPredictionDto? AcademicPrediction { get; set; }

        /// <summary>
        /// Cross-upload progression built from the account's stored history, passed to the
        /// analysis prompt as long-term evidence. Null when the learner has only one period.
        /// </summary>
        public ProgressionDto? Progression { get; set; }
    }
}
