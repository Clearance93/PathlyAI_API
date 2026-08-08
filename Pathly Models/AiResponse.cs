namespace Pathly_Models
{
    public class AiResponse
    {
        public Guid AiResponseId { get; set; }

        public string? UserFullName { get; set; }

        public string? Grade { get; set; }
        
        public string? Summary { get; set; }

        public ApsAnalysis? ApsAnalysis { get; set; }
        public Guid ApsAnalysisId { get; set; }

        public string? ResponseJson { get; set; }

        /// <summary>
        /// SHA-256 fingerprint of the normalized subject/mark set (+ study level) that produced this
        /// response. Used to serve repeat requests for the same academic record straight from the
        /// database instead of paying for another LLM call. Not a security hash — just a cache key.
        /// </summary>
        public string? SubjectSetHash { get; set; }

        public double OverallScore { get; set; }

        public string? AcademicPersonality { get; set; }

        public string? FeedBack { get; set; }

        public string? UserStrength { get; set; }

        public string? UserWeaknesses { get; set; }

        public string? MotivationalMessage { get; set; }

        public List<SubjectResults>? SubjectResults { get; set; }

        public List<CareerMatch>? Top5BestCareers { get; set; }

        public List<DemandingCareerAssessment>? DemandingCareers { get; set; }

        public List<DyingCareerWarning>? DyingCareerWarnings { get; set; }

        public List<EmploymentOutlook>? EmploymentOutlooks { get; set; }

        public List<string>? UniversitiestoConsider { get; set; }

        public List<string>? BursariesAvailable { get; set; }

        public string? StudyTips { get; set; }

        public string? ImprovementtoRoadmap { get; set; }

        public List<string>? SkillsToLearn { get; set; }

        public string? FiveYearsOutLook { get; set; }

        public string? SalaryRange { get; set; }

        public string? RiskAssessment { get; set; }
        
        public string? TeacherRecommendation { get; set; }

        public string? ParentSummary { get; set; }

        public List<string>? SubjectChangeSuggestion { get; set; }

        public DateTime TimeStamp { get; set; }

        public DateTime AddedAt { get; set; }
    }
}
