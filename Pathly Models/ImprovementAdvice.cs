using System.ComponentModel.DataAnnotations;

namespace Pathly_Models
{
    public class ImprovementAdvice
    {
        [Key]
        public Guid ImprovementAdviceId { get; set; }

        public bool ShouldReWriteMatric { get; set; }

        public bool ShouldUpgradeSubjects { get; set; }

        public List<string>? RecommendedSubjecrsToImprove { get; set; }

        public List<string>? AlternativeOptions { get; set; }

        public string? MotivationalGuidance { get; set; }

        public DateTime AddedAt { get; set; }
    }
}
