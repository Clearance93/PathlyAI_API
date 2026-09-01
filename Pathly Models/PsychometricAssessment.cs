using System.ComponentModel.DataAnnotations;

namespace Pathly_Models
{
    /// <summary>
    /// One completed psychometric assessment session (the actual questions the learner answered,
    /// not just the resulting RIASEC scores). Linked to the logged-in user and to the computed
    /// <see cref="PsychometricProfile"/>. The exact answer fingerprint lets us detect a repeated
    /// identical submission so the same stored result can be reused instead of recomputed.
    /// </summary>
    public class PsychometricAssessment
    {
        [Key]
        public Guid PsychometricAssessmentId { get; set; }

        public string ApplicationUserId { get; set; } = string.Empty;

        public ApplicationUser? ApplicationUser { get; set; }

        public Guid PsychometricProfileId { get; set; }

        public PsychometricProfile? PsychometricProfile { get; set; }

        /// <summary>JSON map of rating question id → 1-5 answer.</summary>
        public string? RatingAnswersJson { get; set; }

        /// <summary>JSON map of true/false question id → boolean answer.</summary>
        public string? TrueFalseAnswersJson { get; set; }

        /// <summary>JSON map of multiple-choice question id → chosen RIASEC key.</summary>
        public string? MultipleChoiceAnswersJson { get; set; }

        public int TotalQuestions { get; set; }

        public int AnsweredQuestions { get; set; }

        /// <summary>
        /// SHA-256 fingerprint of this submission's exact answers. Identical answers from the same
        /// user always produce the same fingerprint, so repeat submissions are detected and the
        /// previously stored assessment is returned without duplicating rows.
        /// </summary>
        public string ResultFingerprint { get; set; } = string.Empty;

        public DateTime CompletedAt { get; set; }
    }
}
