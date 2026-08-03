using Pathly_DTOs;

public class AiResponseDto
{
    public double OverallScore { get; set; }

    public string? AcademicPersonality { get; set; }

    public string? Summary { get; set; }

    public string? FeedBack { get; set; }

    public string? MotivationalMessage { get; set; }

    public List<string>? UserStrength { get; set; }

    public List<string>? UserWeaknesses { get; set; }

    public List<string>? StudyTips { get; set; }

    public List<string>? SkillsToLearn { get; set; }

    public string? FiveYearsOutLook { get; set; }

    public string? SalaryRange { get; set; }

    public string? RiskAssessment { get; set; }

    public string? TeacherRecommendation { get; set; }

    public string? ParentSummary { get; set; }

    public string? SubjectChangeSuggestion { get; set; }

    public List<string>? ImprovementtoRoadmap { get; set; }  
    
    public ApsAnalysisDto? ApsAnalysis { get; set; }
    
    public List<SubjectResultsDto>? SubjectResults { get; set; }
    
    public List<CareerMatchDto>? Top3BestCareers { get; set; }
    
    public List<CareerMatchDto>? AlternativeCareers { get; set; }
    
    public List<DemandingCareerAssessmentDto>? DemandingCareers { get; set; }
    
    public List<DyingCareerWarningDto>? DyingCareerWarnings { get; set; }
    
    public List<EmploymentOutlookDto>? EmploymentOutlooks { get; set; }

    public string? ResponseJson { get; set; } 

    public List<string>? BursariesAvailable { get; set; }
    
    public List<string>? UniversitiestoConsider { get; set; }
}