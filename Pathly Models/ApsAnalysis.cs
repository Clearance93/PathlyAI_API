using System.ComponentModel.DataAnnotations;

namespace Pathly_Models
{
    public class ApsAnalysis
    {
        [Key]
        public Guid ApsAnalysisId { get; set; }

        public int CalculatedAps { get; set; }

        public string? ApsExplanation { get; set; }

        public bool QualifiesForUniversity { get; set; }

        public string? QualificationMessage { get; set; }

        public List<string>? UniversitiesTheyQualifyFor { get; set; }

        public List<string>? UniversitiestheyDoNotQualifyFor { get; set; }

        public ImprovementAdvice? ImprovementAdvice { get; set; }
        public Guid ImprovementId { get; set; }

        public DateTime AddedAt { get; set; }
    }
}
