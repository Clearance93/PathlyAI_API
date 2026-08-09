namespace Pathly_Helper
{
    /// <summary>
    /// Configurable weights for combining career evidence dimensions into a single
    /// OverallScore (Part 10). Bind this from appsettings.json (section "CareerMatchWeights")
    /// rather than hardcoding weights inside CareerEvidenceService.
    ///
    /// Weights are relative, not required to sum to any particular total — CareerEvidenceService
    /// normalizes by the sum of the weights that were actually applicable (Psychometric weight is
    /// excluded from the denominator when no psychometric profile was supplied).
    /// </summary>
    public class CareerMatchWeightsOptions
    {
        public double AcademicFit { get; set; } = 0.35;

        public double SubjectAlignment { get; set; } = 0.20;

        public double PsychometricFit { get; set; } = 0.20;

        public double CareerDemand { get; set; } = 0.15;

        public double FutureGrowth { get; set; } = 0.10;
    }
}
