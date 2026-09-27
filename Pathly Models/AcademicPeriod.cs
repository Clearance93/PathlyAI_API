using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pathly_Models
{
    /// <summary>
    /// Persisted term/period block for an <see cref="ExtractedAcademicRecord"/>. One row per
    /// populated term block on the uploaded document, so full per-term history survives
    /// persistence and term-over-term aggregation is possible.
    /// </summary>
    public class AcademicPeriod
    {
        [Key]
        public Guid AcademicPeriodId { get; set; }

        public int Ordinal { get; set; }

        public string? Label { get; set; }

        public bool IsFinal { get; set; }

        public Guid ExtractedAcademicRecordId { get; set; }

        [ForeignKey(nameof(ExtractedAcademicRecordId))]
        public ExtractedAcademicRecord? ExtractedAcademicRecord { get; set; }

        public List<ExtractedSubject> Subjects { get; set; } = new();
    }
}
