using System.ComponentModel.DataAnnotations;

namespace Pathly_Models
{
    public class ExtractedAcademicRecord
    {
        [Key]
        public Guid ExtractionAcademicRecordId { get; set; }

        public string? StudentName { get; set; }

        public string? InstitutionName { get; set; }

        public string? InstitutionType { get; set; }

        public string? StudyLevel { get; set; }

        public List<ExtractedSubject> Subjects { get; set; } = new();

        public List<AcademicPeriod> AcademicPeriods { get; set; } = new();

        public string? RawExtractedText { get; set; }

        public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

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
    }
}
