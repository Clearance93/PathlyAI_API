namespace Pathly_DTOs
{
    /// <summary>
    /// The deterministic evidence computed for one career against one learner's profile
    /// (Part 9/10/11). This is what gets handed to the AI so it can explain a recommendation
    /// using real evidence instead of inventing one. Each dimension is 0-100; <see cref="OverallScore"/>
    /// is the configurable weighted combination of the dimensions that were available (Psychometric
    /// dimensions are only included when a psychometric profile was supplied).
    /// </summary>
    public class CareerEvidenceDto
    {
        public string CareerName { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public int AcademicFit { get; set; }

        public int SubjectAlignment { get; set; }

        /// <summary>Null when no psychometric profile was supplied (academic-only / Layer 1).</summary>
        public int? PsychometricFit { get; set; }

        public int CareerDemand { get; set; }

        public int FutureGrowth { get; set; }

        public double OverallScore { get; set; }

        public string? Description { get; set; }
    }
}
