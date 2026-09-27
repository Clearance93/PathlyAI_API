using Pathly_DTOs;

namespace Pathly_Interfaces.IService
{
    public interface IApsCalculationService
    {
        /// <summary>Legacy single-snapshot calculation — treats the subject list as a final/overall result.</summary>
        ApsResultDto CalculateAPS(List<ExtractedSubjectDto> subjects);

        /// <summary>
        /// APS calculation that is aware of whether the driving result block is final and whether
        /// the study level is a final secondary year. When the driver is non-final (a mid-year term
        /// or a pre-Grade-12 report) the result uses indicative standing wording rather than a hard
        /// qualification verdict — South African universities admit on final NSC results, not a
        /// Grade 10/11 term.
        /// </summary>
        ApsResultDto CalculateAPS(List<ExtractedSubjectDto> subjects, bool isFinal, bool isTertiaryOrAdult, string? studyLevel);

        string GetApsExplanation(int aps);
    } 
}
