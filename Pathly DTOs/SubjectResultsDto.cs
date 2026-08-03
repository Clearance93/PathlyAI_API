namespace Pathly_DTOs
{
    public class SubjectResultsDto
    {
        public Guid SubjectResultId { get; set; }

        public string? Subject { get; set; }

        public int Mark { get; set; }

        public string? Grade { get; set; }

        public string? CareerRelevance { get; set; }

        public string? ImprovementTip { get; set; }

        public DateTime AddedAt { get; set; }
    }
}