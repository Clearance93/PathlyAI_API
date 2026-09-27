namespace Pathly_DTOs
{
    public class UniversityQualificationDto
    {
        public Guid UniversityQualificationId { get; set; }

        public string? Name { get; set; }

        public int MinimumAps { get; set; }

        public string? Status { get; set; }

        public List<string>? RecommendedCourse { get; set; }

        public int Gap { get; set; }

        public DateTime AddedAt { get; set; }
    }
}
