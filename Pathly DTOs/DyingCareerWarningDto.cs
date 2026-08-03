namespace Pathly_DTOs
{
    public class DyingCareerWarningDto
    {
        public Guid Id { get; set; }

        public string? CareerTitle { get; set; }

        public string? WhyItIsDying { get; set; }

        public int JobAvailabilityIn5Years { get; set; }

        public int ChanceOfGettingJobAfterStudying { get; set; }

        public string? HonestWarning { get; set; }

        public string? MotivationalRedirect { get; set; }

        public string? BetterAlternative { get; set; }

        public bool IsRelevantToStudent { get; set; }

        public string? RelevanceReason { get; set; }
    }
}