using System.ComponentModel.DataAnnotations;

namespace Pathly_Models
{
    public class CareerMatch
    {
        [Key]
        public Guid CareerMatchId { get; set; }

        public string? Title { get; set; }

        public string? Reason { get; set; }

        public string? Field { get; set; }

        public int MatchPercentage { get; set; }

        public string? requiredSubjects { get; set; }

        public string? UniversityCourse { get; set; }

        public string? JobDescription { get; set; }

        public string? growthPotentials { get; set; }

        public string? SalaryRange { get; set; }

        public string? TimeToQualify { get; set; }

        public List<string>? TopCompaniesHiring { get; set; }

        public DateTime AddedAt { get; set; }
    }
}