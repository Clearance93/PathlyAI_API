namespace Pathly_DTOs
{
    /// <summary>
    /// Request shape for the combined academic + psychometric analysis when the academic record
    /// has ALREADY been extracted (Layer 1). Reuses the stored extraction instead of forcing the
    /// learner to re-upload their file, and optionally links the result to the logged-in user.
    /// </summary>
    public class PsychometricAnalysisRequestDto
    {
        public string ExtractionAcademicRecordId { get; set; } = string.Empty;

        /// <summary>Optional id of the logged-in user taking the assessment, for result linkage.</summary>
        public string? UserId { get; set; }

        public PsychometricProfileDto Psychometric { get; set; } = new();
    }
}
