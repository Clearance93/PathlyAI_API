using System.ComponentModel.DataAnnotations;

namespace Pathly_Models
{
    /// <summary>
    /// Canonical, reusable subject record (Part 2/15 — "subject knowledge layer").
    /// One row per real-world subject, deduplicated regardless of casing/whitespace/formatting
    /// differences across uploaded documents. Distinct from a learner's actual result for a
    /// subject (<see cref="ExtractedSubject"/>/<see cref="SubjectResults"/>).
    /// </summary>
    public class Subject
    {
        [Key]
        public Guid SubjectId { get; set; }

        [MaxLength(200)]
        public string CanonicalName { get; set; } = string.Empty;

        /// <summary>Lowercased, whitespace-collapsed form used for lookup/dedup.</summary>
        [MaxLength(200)]
        public string NormalizedName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
