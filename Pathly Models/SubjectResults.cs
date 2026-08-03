using System.ComponentModel.DataAnnotations;

namespace Pathly_Models
{
    public class SubjectResults
    {
        [Key]
        public Guid SubjectResultId { get; set; }

        public string? Subject { get; set; }

        public int Mark { get; set; }
        
        public string? Grade { get; set; }

        public string? CareerRelevance { get; set; }

        public string? ImprovementTip { get; set; }

        public DateTime AddedAt { get; set; }

        public Guid AcademicRecordId { get; set; }

        public virtual AcademicRecords? AcademicRecord { get; set; }
    }
}