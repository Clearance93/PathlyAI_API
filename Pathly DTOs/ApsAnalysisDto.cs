namespace Pathly_DTOs
{
    public class ApsAnalysisDto
    {
        public Guid ApsAnalysisId { get; set; }

        public int CalculatedAps { get; set; }

        public string? ApsExplanation { get; set; }

        public bool QualifiesForUniveisty { get; set; }

        public string? QualificationMessage { get; set; }

        public List<UniversityQualificationDto>? UniversitiesTheyQualifyFor { get; set; }

        public List<UniversityQualificationDto>? UniversitiesTheyDoNotQualifyFor { get; set; }

        public ImprovementAdviceDto? ImprovementAdvice { get; set; }
        public Guid ImprovementId { get; set; }

        public DateTime AddedAt { get; set; }
    }
}