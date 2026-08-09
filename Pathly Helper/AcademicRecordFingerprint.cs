using System.Security.Cryptography;
using System.Text;
using Pathly_DTOs;

namespace Pathly_Helper
{
    /// <summary>
    /// Builds a stable, EXACT cache key for an extracted academic record based only on the
    /// information that actually changes the AI analysis: the study level and the exact set
    /// of subjects + marks (+ optional analysis/prompt version and psychometric fingerprint).
    /// Student name, institution, and file metadata are deliberately excluded, because two
    /// different learners with the same subject results should be able to share the same
    /// cached analysis.
    ///
    /// This is intentionally NOT fuzzy: marks are never rounded or bucketed, subject names are
    /// only normalized for whitespace/casing (never fuzzy-matched to "similar" subjects), and a
    /// single changed mark or subject always produces a different hash.
    /// </summary>
    public static class AcademicRecordFingerprint
    {
        /// <summary>
        /// Current analysis reasoning version. Bump this whenever Pathly's recommendation logic
        /// changes materially, so old cached results stop being served automatically.
        /// </summary>
        public const string CurrentAnalysisVersion = "1.0";

        public static string ComputeHash(
            ExtractedAcademicRecordDto record,
            string analysisVersion = CurrentAnalysisVersion,
            string? promptVersion = null,
            string? psychometricFingerprint = null)
        {
            if (record is null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            var normalizedSubjects = record.Subjects
                .Select(NormalizeSubject)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();

            var studyLevel = SubjectNormalizer.Normalize(record.StudyLevel);
            var effectivePromptVersion = string.IsNullOrWhiteSpace(promptVersion)
                ? GroqPromptBuilder.PromptVersion
                : promptVersion;
            var psychometricPart = string.IsNullOrWhiteSpace(psychometricFingerprint)
                ? "none"
                : psychometricFingerprint;

            var fingerprint =
                $"analysisVersion:{analysisVersion}|" +
                $"promptVersion:{effectivePromptVersion}|" +
                $"level:{studyLevel}|" +
                $"subjects:{string.Join(",", normalizedSubjects)}|" +
                $"psychometric:{psychometricPart}";

            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint));

            return Convert.ToHexString(hashBytes);
        }

        private static string NormalizeSubject(ExtractedSubjectDto subject)
        {
            var name = SubjectNormalizer.Normalize(subject.SubjectName);
            var markType = SubjectNormalizer.Normalize(subject.MarkType);

            // Prefer the numeric mark when we have one (covers both real percentages and
            // Cambridge-style grade-equivalent estimates). Fall back to the raw symbol so
            // ungraded/incomplete rows still contribute to the fingerprint distinctly.
            // MarkType is included so a real 80% and a Cambridge "A" (estimated at 80%)
            // never collapse into the same cache entry. The exact numeric mark is used —
            // never rounded or bucketed — so e.g. 74% and 75% always produce different hashes.
            var value = subject.NumericMark.HasValue
                ? subject.NumericMark.Value.ToString()
                : SubjectNormalizer.Normalize(subject.Symbol);

            return $"{name}:{markType}:{value}";
        }
    }
}
