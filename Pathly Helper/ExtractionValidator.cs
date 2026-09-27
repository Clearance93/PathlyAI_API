using System.Text.RegularExpressions;
using Pathly_DTOs;

namespace Pathly_Helper
{
    public class ExtractionValidationResult
    {
        public bool IsValid => Errors.Count == 0;

        /// <summary>Blocking problems � worth an automatic retry against Groq.</summary>
        public List<string> Errors { get; } = new();

        /// <summary>Non-blocking concerns � surfaced to the caller, but not worth retrying over.</summary>
        public List<string> Warnings { get; } = new();
    }

    /// <summary>
    /// Deterministic, rule-based sanity checks run against whatever
    /// <see cref="Pathly_DTOs.ExtractedAcademicRecordDto"/> the Groq structuring step produced.
    /// This is the safety net that makes the free extraction pipeline trustworthy: an LLM can
    /// hallucinate a mark, miss a subject, or duplicate a row, and none of that shows up as an
    /// HTTP error � it just looks like a normal response. These checks catch the shapes of
    /// mistake that are actually detectable without a human, without another AI call, and
    /// without any external service. Anything they can't confirm is left to
    /// <see cref="ExtractedAcademicRecordDto.NeedsManualReview"/> rather than guessed at.
    /// </summary>
    public static class ExtractionValidator
    {
        // A rough heuristic for "this line probably contains a subject's mark": a percentage, a
        // standalone Cambridge-style letter grade, or a two/three-digit number on its own. Used
        // only to sanity-check the subject COUNT, never to extract data itself.
        private static readonly Regex MarkLikeLine = new(
            @"(\d{1,3}\s*%)|(\b[A-E][*]?\b)|(\b\d{2,3}\b)",
            RegexOptions.Compiled);

        public static ExtractionValidationResult Validate(string rawText, ExtractedAcademicRecordDto record)
        {
            var result = new ExtractionValidationResult();

            // Flatten the FULL extraction: every subject across every reported period. When the
            // record has no periods (legacy flat single-snapshot), the driver Subjects are used.
            var allSubjects = record.AcademicPeriods.Count > 0
                ? record.AcademicPeriods.SelectMany(p => p.Subjects).ToList()
                : record.Subjects;

            if (allSubjects.Count == 0)
            {
                result.Errors.Add("No subjects were extracted from the document.");
            }

            // Per-term validation. A subject legitimately appears once per reported term, so
            // "duplicate subject" is an error only WITHIN a term, never across terms � the old
            // flat duplicate check made the retry loop silently delete an entire term.
            var subjectsByTerm = allSubjects
                .Select(s => (Key: TermKey(s), Subject: s))
                .GroupBy(x => x.Key, StringComparer.Ordinal);

            foreach (var termGroup in subjectsByTerm)
            {
                // Dedupe on the CANONICAL name so "Maths" and "Mathematics" (or "Maths Term 1"
                // and "Mathematics Term 1") within one term are caught as the same subject.
                var seenSubjectNames = new HashSet<string>(StringComparer.Ordinal);

                foreach (var (_, subject) in termGroup)
                {
                    if (string.IsNullOrWhiteSpace(subject.SubjectName))
                    {
                        result.Errors.Add("A subject row is missing its name.");
                        continue;
                    }

                    // A "subject" that is really a header, a bare mark, or a stray word must not
                    // be trusted — this is the check that rejects things like "napping".
                    if (!SubjectCanonicalizer.IsRecognisedSubject(subject.SubjectName))
                    {
                        result.Errors.Add(
                            $"\"{subject.SubjectName}\" does not look like a real school subject.");
                    }

                    var canonical = SubjectCanonicalizer.ToCanonical(subject.SubjectName);

                    if (!seenSubjectNames.Add(canonical))
                    {
                        result.Errors.Add(
                            $"Duplicate subject within {termGroup.Key}: \"{subject.SubjectName}\".");
                    }

                    if (subject.NumericMark is < 0 or > 100)
                    {
                        result.Errors.Add($"\"{subject.SubjectName}\" has an out-of-range mark: {subject.NumericMark}.");
                    }

                    if (subject.NumericMark is null && string.IsNullOrWhiteSpace(subject.Symbol))
                    {
                        result.Warnings.Add($"\"{subject.SubjectName}\" has neither a numeric mark nor a grade symbol.");
                    }
                }
            }

            // Soft coverage check: if a term block in the raw text has noticeably more mark-like
            // lines than the subjects we extracted, some subjects were probably missed. Heuristic
            // by nature (headers, totals, and university-reference tables also match), so it only
            // fires on a large gap rather than any mismatch at all.
            var markLikeLineCount = rawText
                .Split('\n')
                .Count(line => MarkLikeLine.IsMatch(line));

            var driverSubjectCount = record.Subjects.Count;

            if (markLikeLineCount >= 6 && driverSubjectCount > 0 &&
                driverSubjectCount < markLikeLineCount / 3)
            {
                result.Warnings.Add(
                    $"Only {driverSubjectCount} driver-block subjects extracted, but the document " +
                    $"appears to contain around {markLikeLineCount} mark-like lines � some subjects may " +
                    $"be missing from the latest/driver term.");
            }

            // Driver metadata sanity. A flat record (no periods) is a legitimate legacy single
            // snapshot � treated as Overall/Final. A multi-period record must have a populated
            // driver and the driver must appear in its period list.
            if (record.AcademicPeriods.Count > 0 && driverSubjectCount == 0)
            {
                result.Errors.Add("No subjects were extracted from the document.");
            }

            return result;
        }

        private static string TermKey(ExtractedSubjectDto subject)
        {
            return subject.IsFinal
                ? "Final"
                : subject.TermOrdinal is > 0
                    ? $"Term {subject.TermOrdinal}"
                    : "Unspecified term";
        }
    }
}
