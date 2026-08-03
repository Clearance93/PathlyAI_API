using System.ComponentModel.DataAnnotations;

namespace Pathly_Models
{
    public class DyingCareerWarning
    {
        [Key]
        public Guid DyingCareerWarningId { get; set; }

        public string? CareerTitle { get; set; }

        public string? WhyItIsDying { get; set; }

        public int JobAvailabilityIn5Years { get; set; }

        public int ChanceOfGettingJobAfterStudying { get; set; }

        public string? Honestwarning { get; set; }

        public string? MotivationalRedirect { get; set; }

        public string? BetterAlternative { get; set; }

        public bool IsRelevanttoStudent { get; set; }

        public string? RelevanceReason { get; set; }

        public DateTime AddedAt { get; set; }
    }
}
