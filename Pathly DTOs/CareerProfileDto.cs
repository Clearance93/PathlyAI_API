namespace Pathly_DTOs
{
    /// <summary>
    /// A career in Pathly's deterministic knowledge base (Part 9/10/15) — NOT an LLM output.
    /// This is the "ground truth" evidence used to compute Academic Fit / Subject Alignment /
    /// Psychometric Fit / Career Demand / Future Growth before the AI ever reasons about it,
    /// so the AI explains real evidence instead of inventing career facts.
    /// </summary>
    public class CareerProfileDto
    {
        public Guid CareerProfileId { get; set; }

        public string CareerName { get; set; } = string.Empty;

        /// <summary>Established, Emerging, Technology, Green/Renewable, etc. (Part 9).</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Comma-separated canonical subject names most relevant to this career.</summary>
        public string RequiredSubjects { get; set; } = string.Empty;

        public int MinimumAps { get; set; }

        // RIASEC weighting (0-100) — how strongly this career aligns with each interest type.
        public int RealisticWeight { get; set; }
        public int InvestigativeWeight { get; set; }
        public int ArtisticWeight { get; set; }
        public int SocialWeight { get; set; }
        public int EnterprisingWeight { get; set; }
        public int ConventionalWeight { get; set; }

        /// <summary>0-100 — how in-demand this career currently is in the SA market.</summary>
        public int DemandScore { get; set; }

        /// <summary>0-100 — how positively this field is expected to grow/change.</summary>
        public int GrowthScore { get; set; }

        public string? Description { get; set; }
    }
}
