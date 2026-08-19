namespace Pathly_DTOs
{
    /// <summary>
    /// The full psychometric submission coming from the assessment UI: every answered question
    /// (rating, true/false and multiple choice) plus the computed RIASEC scores, tied to the
    /// id of the user who was logged on when they took the assessment.
    /// </summary>
    public class PsychometricSubmissionDto
    {
        /// <summary>Id of the logged-in user (ApplicationUser.Id) who completed the assessment.</summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>Rating question id → 1-5 answer.</summary>
        public Dictionary<string, int> RatingAnswers { get; set; } = new();

        /// <summary>True/False question id → boolean answer.</summary>
        public Dictionary<string, bool> TrueFalseAnswers { get; set; } = new();

        /// <summary>Multiple-choice question id → chosen RIASEC key (R/I/A/S/E/C).</summary>
        public Dictionary<string, string> MultipleChoiceAnswers { get; set; } = new();

        /// <summary>The RIASEC scores the UI computed for this exact answer set.</summary>
        public PsychometricProfileDto Profile { get; set; } = new();
    }
}
