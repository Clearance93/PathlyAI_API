using System.Text.RegularExpressions;
using Pathly_DTOs;

namespace Pathly_Helper
{
    public class ExtractionValidationResult
    {
        public bool IsValid => Errors.Count == 0;

        /// <summary>Blocking problems — worth an automatic retry against Groq.</summary>
        public List<string> Errors { get; } = new();

        /// <summary>Non-blocking concerns — surfaced to the caller, but not worth retrying over.</summary>
        public List<string> Warnings { get; } = new();
    }

    /// <summary>
    /// Deterministic, rule-based sanity checks run against whatever
    /// <see cref="Pathly_DTOs.ExtractedAcademicRecordDto"/> the Groq structuring step produced.
    /// This is the safety net that makes the free extraction pipeline trustworthy: an LLM can
    /// hallucinate a mark, miss a subject, or duplicate a row, and none of that shows up as an
    /// HTTP error — it just looks like a normal response. These checks catch the shapes of
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

            if (record.Subjects.Count == 0)
            {
                result.Errors.Add("No subjects were extracted from the document.");
            }

            var seenSubjectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var subject in record.Subjects)
            {
                if (string.IsNullOrWhiteSpace(subject.SubjectName))
                {
                    result.Errors.Add("A subject row is missing its name.");
                    continue;
                }

                if (!seenSubjectNames.Add(subject.SubjectName.Trim()))
                {
                    result.Errors.Add($"Duplicate subject: \"{subject.SubjectName}\".");
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

            // Soft coverage check: if the raw text has noticeably more mark-like lines than the
            // number of subjects we extracted, some subjects were probably missed. This is
            // heuristic by nature (headers, totals, and university-reference tables also match
            // the pattern), so it only fires on a large gap rather than any mismatch at all —
            // the goal is to catch "extracted 2 of 9 subjects", not nitpick off-by-ones. Kept as
            // a warning, not an error: retrying won't fix a document that's genuinely short on
            // recognizable mark lines, so it's surfaced for a human instead of looping forever.
            var markLikeLineCount = rawText
                .Split('\n')
                .Count(line => MarkLikeLine.IsMatch(line));

            if (markLikeLineCount >= 6 && record.Subjects.Count > 0 &&
                record.Subjects.Count < markLikeLineCount / 3)
            {
                result.Warnings.Add(
                    $"Only {record.Subjects.Count} subjects extracted, but the document appears to " +
                    $"contain around {markLikeLineCount} mark-like lines — some subjects may be missing.");
            }

            return result;
        }
    }
}
