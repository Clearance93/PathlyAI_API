namespace Pathly_DTOs
{
    /// <summary>
    /// Stored psychometric assessment as returned by the storage API — includes both the raw
    /// answers and the resulting profile, so the UI can restore a learner's last results.
    /// </summary>
    public class PsychometricAssessmentDto
    {
        public Guid PsychometricAssessmentId { get; set; }

        public string ApplicationUserId { get; set; } = string.Empty;

        public PsychometricProfileDto Profile { get; set; } = new();

        public Dictionary<string, int> RatingAnswers { get; set; } = new();

        public Dictionary<string, bool> TrueFalseAnswers { get; set; } = new();

        public Dictionary<string, string> MultipleChoiceAnswers { get; set; } = new();

        public int TotalQuestions { get; set; }

        public int AnsweredQuestions { get; set; }

        /// <summary>True when an identical submission already existed and no new row was written.</summary>
        public bool ServedFromExisting { get; set; }

        public DateTime CompletedAt { get; set; }
    }
}
