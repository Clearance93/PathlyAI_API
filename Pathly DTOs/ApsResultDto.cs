namespace Pathly_DTOs
{
    public class ApsResultDto
    {
        public int TotalAps { get; set; }

        public bool IncludesLifeOrientation { get; set; }

        public double AverageMark { get; set; }

        public int Distinctions { get; set; }

        public string QualificationLevel { get; set; } = string.Empty;

        public List<SubjectApsDto> Subjects { get; set; } = new();
    }
}
