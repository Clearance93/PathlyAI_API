using Pathly_DTOs;

public class AiResponseDto
{
    /// <summary>Id of the persisted analysis row, so the UI can re-open this exact report later.</summary>
    public Guid AiResponseId { get; set; }

    /// <summary>The extracted academic record this analysis came from (used for premium combination).</summary>
    public Guid? ExtractionAcademicRecordId { get; set; }

    /// <summary>Label of the term/final block used as the driver for this analysis.</summary>
    public string? DriverTermLabel { get; set; }

    /// <summary>True for Layer 2 (academic + psychometric) analyses, false for academic-only.</summary>
    public bool IsPremium { get; set; }

    /// <summary>When this report was generated (UTC).</summary>
    public DateTime? GeneratedAt { get; set; }

    /// <summary>Deterministic next-term projection, shown to the learner alongside the report.</summary>
    public AcademicPredictionDto? AcademicPrediction { get; set; }

    /// <summary>
    /// Longitudinal progression across every upload for this account (termly/yearly trends),
    /// so a new upload builds on the learner's history instead of discarding it.
    /// </summary>
    public ProgressionDto? Progression { get; set; }

    /// <summary>True when the free extraction pipeline flagged the underlying record for manual review.</summary>
    public bool NeedsManualReview { get; set; }

    /// <summary>Human-readable extraction warnings worth surfacing to the learner.</summary>
    public List<string>? ExtractionWarnings { get; set; }

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

    /// <summary>
    /// Academic-only (Layer 1) reports use this to encourage the learner to complete the
    /// premium psychometric assessment (Part 8). Left null on premium (Layer 2) reports,
    /// which already combine both.
    /// </summary>
    public string? PsychometricUpsellMessage { get; set; }

    /// <summary>
    /// True when the learner's stored career-interest (RIASEC) profile was folded into this
    /// report. For a standard (non-premium) analysis this happens automatically once the learner
    /// has completed the assessment, so the quiz genuinely influences the dashboard.
    /// </summary>
    public bool PsychometricIncluded { get; set; }

    /// <summary>
    /// The deterministic career evidence (Part 9/10) that grounded this report's career
    /// recommendations. Populated by Pathly before the AI call, not invented by the AI.
    /// </summary>
    public List<CareerEvidenceDto>? CareerEvidence { get; set; }
}
