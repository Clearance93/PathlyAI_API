namespace Pathly_DTOs
{
    /// <summary>
    /// A learner's psychometric profile using RIASEC (Holland Code) interest dimensions,
    /// each scored 0-100. This is the premium input (Part 7/13) that, combined with the
    /// academic profile, produces the deeper "academic + psychometric" analysis.
    /// </summary>
    public class PsychometricProfileDto
    {
        public Guid PsychometricProfileId { get; set; }

        public int Realistic { get; set; }

        public int Investigative { get; set; }

        public int Artistic { get; set; }

        public int Social { get; set; }

        public int Enterprising { get; set; }

        public int Conventional { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
