namespace Pathly_DTOs
{
    public class AcademicRecordDtos
    {
        public string? StudentId { get; set; }

        public string? StudentName { get; set; }

        public string? Grade { get; set; }

        public List<CreateSubjectDto>? Subjects { get; set; }
    }
}