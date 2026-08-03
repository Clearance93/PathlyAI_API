using System.ComponentModel.DataAnnotations;

namespace Pathly_Models
{
    public class ExtractedSubject
    {
        [Key]
        public Guid ExtractionSubjectId { get; set; }

        public string? SubjectName { get; set; }

        public string? RawMark { get; set; }

        public int? NumericMark { get; set; }

        public string? Symbol { get; set; }

        public string? MarkType { get; set; }
    }
}