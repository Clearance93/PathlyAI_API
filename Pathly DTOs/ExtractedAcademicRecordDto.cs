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
    }
}
