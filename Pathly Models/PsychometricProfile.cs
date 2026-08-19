using System.ComponentModel.DataAnnotations;

namespace Pathly_Models
{
    /// <summary>
    /// A learner's RIASEC psychometric profile (Part 7/13/15). Kept as its own entity, separate
    /// from the academic profile, so the two can be combined or reasoned about independently.
    /// </summary>
    public class PsychometricProfile
    {
        [Key]
        public Guid PsychometricProfileId { get; set; }

        /// <summary>
        /// The logged-in learner (ApplicationUser.Id) who completed the assessment that produced
        /// this profile. Null only for legacy rows created before user linkage existed.
        /// </summary>
        public string? ApplicationUserId { get; set; }

        public ApplicationUser? ApplicationUser { get; set; }

        public int Realistic { get; set; }

        public int Investigative { get; set; }

        public int Artistic { get; set; }

        public int Social { get; set; }

        public int Enterprising { get; set; }

        public int Conventional { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
