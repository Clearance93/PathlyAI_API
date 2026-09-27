using Pathly_DTOs;

namespace Pathly_Helper
{
    /// <summary>
    /// Deterministic post-extraction clean-up. Runs after the structuring/validation step and
    /// BEFORE any APS or career calculation, so garbage that survived the LLM (a stray verb like
    /// "napping", a "Total" header, duplicate rows for the same subject within one term) can
    /// never be treated as a scored subject and skew the APS or inflate career matches.
    ///
    /// This is deliberately code, not model output: it assigns each subject its canonical name,
    /// drops rows that are not recognisable subjects, and merges duplicates within a term. Every
    /// removal is reported back as a human-readable warning.
    /// </summary>
    public static class AcademicRecordSanitizer
    {
        /// <summary>A real SA/Cambridge report has ~7-12 subjects; anything far beyond that is OCR noise.</summary>
        private const int MaxSubjectsPerPeriod = 20;

        /// <summary>Cleans the record in place and returns warnings describing what was changed.</summary>
        public static List<string> Sanitize(ExtractedAcademicRecordDto record)
        {
            if (record is null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            var warnings = new List<string>();

            SanitizeSubjectList(record.Subjects, "the latest/driver block", warnings);

            foreach (var period in record.AcademicPeriods)
            {
                var label = string.IsNullOrWhiteSpace(period.Label) ? $"Term {period.Ordinal}" : period.Label!;
                SanitizeSubjectList(period.Subjects, label, warnings);
            }

            return warnings;
        }

        private static void SanitizeSubjectList(List<ExtractedSubjectDto> subjects, string label, List<string> warnings)
        {
            if (subjects is null || subjects.Count == 0)
            {
                return;
            }

            var kept = new List<ExtractedSubjectDto>();
            var seenCanonical = new HashSet<string>(StringComparer.Ordinal);
            var removed = new List<string>();
            var duplicates = new List<string>();

            foreach (var subject in subjects)
            {
                var canonical = SubjectCanonicalizer.ToCanonical(subject.SubjectName);
                subject.CanonicalSubjectName = canonical;

                if (!SubjectCanonicalizer.IsRecognisedSubject(subject.SubjectName))
                {
                    removed.Add(string.IsNullOrWhiteSpace(subject.SubjectName) ? "(blank)" : subject.SubjectName!);
                    continue;
                }

                if (!seenCanonical.Add(canonical))
                {
                    duplicates.Add(subject.SubjectName ?? canonical);
                    continue;
                }

                kept.Add(subject);
            }

            subjects.Clear();

            if (kept.Count > MaxSubjectsPerPeriod)
            {
                warnings.Add(
                    $"{label} contained {kept.Count} subjects — only the first {MaxSubjectsPerPeriod} were kept. " +
                    "Please double-check the extracted subjects against the original report.");
            }

            subjects.AddRange(kept.Take(MaxSubjectsPerPeriod));

            if (removed.Count > 0)
            {
                warnings.Add(
                    $"{removed.Count} row(s) in {label} did not look like real subjects and were ignored: " +
                    $"{string.Join(", ", removed.Take(5))}{(removed.Count > 5 ? ", …" : string.Empty)}.");
            }

            if (duplicates.Count > 0)
            {
                warnings.Add(
                    $"{duplicates.Count} duplicate subject row(s) were merged within {label}: " +
                    $"{string.Join(", ", duplicates.Take(5))}{(duplicates.Count > 5 ? ", …" : string.Empty)}.");
            }
        }
    }
}
