using Pathly_DTOs;

namespace PathlyInterfaces.IService
{
    /// <summary>
    /// Computes deterministic, explainable career evidence (Part 9/10/11) from Pathly's own
    /// career knowledge base against a learner's academic profile and (optionally) their
    /// psychometric profile. This runs BEFORE any AI call — the AI explains this evidence,
    /// it does not invent it.
    /// </summary>
    public interface ICareerEvidenceService
    {
        Task<List<CareerEvidenceDto>> ComputeEvidenceAsync(
            ExtractedAcademicRecordDto academicRecord,
            ApsResultDto apsResult,
            PsychometricProfileDto? psychometricProfile = null,
            int topN = 10);
    }
}
