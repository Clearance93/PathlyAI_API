using System.ComponentModel.DataAnnotations;

namespace Pathly_Models
{
    public class AcademicRecords
    {
        [Key]
        public Guid AcadmicRecordId { get; set; }

        public string? StudentId { get; set; }

        public string? StudentName { get; set; }

        public string? Grade { get; set; }

        public int ClalculatedAPS { get; set; }

        public DateTime UploadedAt { get; set; }

        public virtual ICollection<SubjectResults>? SubjectResults { get; set; }
    }
}   