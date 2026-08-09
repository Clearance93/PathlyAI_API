namespace Pathly_DTOs
{
    /// <summary>
    /// A canonical, reusable subject record (the "subject knowledge layer" — Part 2/15).
    /// Not to be confused with a learner's actual result for a subject, which lives on
    /// <see cref="ExtractedSubjectDto"/>/<see cref="SubjectResultsDto"/>.
    /// </summary>
    public class SubjectDto
    {
        public Guid SubjectId { get; set; }

        public string CanonicalName { get; set; } = string.Empty;

        public string NormalizedName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
