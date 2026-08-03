using System.ComponentModel.DataAnnotations;

namespace Pathly_Models
{
    public class EmploymentOutlook
    {
        [Key]
        public Guid EmploymentOutlookId { get; set; }

        public string? CareerTitle { get; set; }

        public int ChanceOfEmploymentAfterGraduation { get; set; }

        public string? AverageTimeToGetFirstJob { get; set; }

        public string? JobMarketCompetition { get; set; }

        public string? SouthAfricanMarketInsight { get; set; }

        public string? GlobalOpportunities { get; set; }

        public List<string>? TopIndustriesHiring { get; set; }

        public string? EntryLevelSalary { get; set; }

        public string? SeniorLevelSalary { get; set; }

        public string? OutlookSummry { get; set; }

        public DateTime AddedAt { get; set; }
    }
}
