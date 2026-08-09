using Pathly_DTOs;

namespace PathlyInterfaces.IService
{
    /// <summary>
    /// The reusable "subject knowledge layer" (Part 2/15). Normalizes subject names and
    /// resolves them to a single canonical <see cref="Pathly_Models.Subject"/> record,
    /// creating one only when it doesn't already exist. This is purely a knowledge/dedup
    /// concern — it has no bearing on personalized AI analysis caching.
    /// </summary>
    public interface ISubjectKnowledgeService
    {
        Task<SubjectDto> GetOrCreateCanonicalSubjectAsync(string? rawSubjectName);

        Task EnsureSubjectsPersistedAsync(IEnumerable<ExtractedSubjectDto> subjects);
    }
}
