using Pathly_Core.Unit;
using Pathly_DTOs;
using Pathly_Helper;
using Pathly_Models;
using PathlyInterfaces.IService;

namespace Pathly_Services
{
    /// <summary>
    /// The reusable subject knowledge layer (Part 2/15). This is deliberately separate from
    /// personalized analysis caching — knowing "Mathematics" already exists in the database
    /// says nothing about what career analysis any particular Mathematics learner should get.
    /// </summary>
    public class SubjectKnowledgeService : ISubjectKnowledgeService
    {
        private readonly IUnitOfWork _Unit;

        public SubjectKnowledgeService(IUnitOfWork unit)
        {
            _Unit = unit ?? throw new ArgumentNullException(nameof(unit));
        }

        public async Task<SubjectDto> GetOrCreateCanonicalSubjectAsync(string? rawSubjectName)
        {
            var normalizedName = SubjectNormalizer.Normalize(rawSubjectName);

            var existing = await _Unit.Subject.FindByNormalizedNameAsync(normalizedName);
            if (existing is not null)
            {
                return ToDto(existing);
            }

            var canonicalName = string.IsNullOrWhiteSpace(rawSubjectName)
                ? "Unknown Subject"
                : rawSubjectName.Trim();

            var subject = new Subject
            {
                SubjectId = Guid.NewGuid(),
                CanonicalName = canonicalName,
                NormalizedName = normalizedName,
                CreatedAt = DateTime.Now
            };

            await _Unit.Subject.AddAsync(subject);
            await _Unit.SaveChangesAsync();

            return ToDto(subject);
        }

        public async Task EnsureSubjectsPersistedAsync(IEnumerable<ExtractedSubjectDto> subjects)
        {
            // Resolve sequentially against the same unit-of-work/DbContext so duplicate subject
            // names within the same document (e.g. two rows that both say "Mathematics") reuse
            // the same in-memory-tracked entity instead of racing to create two rows.
            foreach (var subject in subjects)
            {
                await GetOrCreateCanonicalSubjectAsync(subject.SubjectName);
            }
        }

        private static SubjectDto ToDto(Subject subject)
        {
            return new SubjectDto
            {
                SubjectId = subject.SubjectId,
                CanonicalName = subject.CanonicalName,
                NormalizedName = subject.NormalizedName,
                CreatedAt = subject.CreatedAt
            };
        }
    }
}
