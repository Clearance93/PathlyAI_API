using System.Security.Cryptography;
using System.Text;
using Pathly_DTOs;

namespace Pathly_Helper
{
    /// <summary>
    /// Builds a stable cache key for an extracted academic record based only on the
    /// information that actually changes the AI analysis: the study level and the
    /// set of subjects + marks. Student name, institution, and file metadata are
    /// deliberately excluded, because two different learners with the same subject
    /// results should be able to share the same cached analysis.
    /// </summary>
    public static class AcademicRecordFingerprint
    {
        public static string ComputeHash(ExtractedAcademicRecordDto record)
        {
            if (record is null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            var normalizedSubjects = record.Subjects
                .Select(NormalizeSubject)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();

            var studyLevel = NormalizeToken(record.StudyLevel);

            var fingerprint = $"level:{studyLevel}|subjects:{string.Join(",", normalizedSubjects)}";

            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint));

            return Convert.ToHexString(hashBytes);
        }

        private static string NormalizeSubject(ExtractedSubjectDto subject)
        {
            var name = NormalizeToken(subject.SubjectName);
            var markType = NormalizeToken(subject.MarkType);

            // Prefer the numeric mark when we have one (covers both real percentages and
            // Cambridge-style grade-equivalent estimates). Fall back to the raw symbol so
            // ungraded/incomplete rows still contribute to the fingerprint distinctly.
            // MarkType is included so a real 80% and a Cambridge "A" (estimated at 80%)
            // never collapse into the same cache entry.
            var value = subject.NumericMark.HasValue
                ? subject.NumericMark.Value.ToString()
                : NormalizeToken(subject.Symbol);

            return $"{name}:{markType}:{value}";
        }

        private static string NormalizeToken(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "unknown"
                : string.Join(' ', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
