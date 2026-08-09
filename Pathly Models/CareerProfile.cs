using System.ComponentModel.DataAnnotations;

namespace Pathly_Models
{
    /// <summary>
    /// Deterministic career knowledge base entry (Part 9/10/15). This is Pathly's own data —
    /// not an LLM output — used to compute explainable evidence for a learner before any AI
    /// call is made. See <see cref="Pathly_DTOs.CareerEvidenceDto"/> for the computed result.
    /// </summary>
    public class CareerProfile
    {
        [Key]
        public Guid CareerProfileId { get; set; }

        [MaxLength(200)]
        public string CareerName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        /// <summary>Comma-separated canonical subject names most relevant to this career.</summary>
        public string RequiredSubjects { get; set; } = string.Empty;

        public int MinimumAps { get; set; }

        public int RealisticWeight { get; set; }
        public int InvestigativeWeight { get; set; }
        public int ArtisticWeight { get; set; }
        public int SocialWeight { get; set; }
        public int EnterprisingWeight { get; set; }
        public int ConventionalWeight { get; set; }

        public int DemandScore { get; set; }

        public int GrowthScore { get; set; }

        public string? Description { get; set; }
    }
}
