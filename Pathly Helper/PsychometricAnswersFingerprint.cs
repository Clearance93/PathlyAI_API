using System.Security.Cryptography;
using System.Text;

namespace Pathly_Helper
{
    /// <summary>
    /// Exact fingerprint for a psychometric assessment SUBMISSION — the full set of answered
    /// questions, not just the resulting scores. Two learners (or the same learner twice) who
    /// answered every question identically produce the same fingerprint, which lets the storage
    /// layer reuse the previously stored assessment instead of duplicating it.
    ///
    /// Deliberately NOT fuzzy: answer keys/values are canonicalized only for stable ordering,
    /// and any single changed answer produces a different hash.
    /// </summary>
    public static class PsychometricAnswersFingerprint
    {
        public static string ComputeHash(
            IReadOnlyDictionary<string, int> ratingAnswers,
            IReadOnlyDictionary<string, bool> trueFalseAnswers,
            IReadOnlyDictionary<string, string> multipleChoiceAnswers)
        {
            var fingerprint =
                $"rating:{Canonicalize(ratingAnswers, v => v.ToString())}|" +
                $"tf:{Canonicalize(trueFalseAnswers, v => v ? "true" : "false")}|" +
                $"mc:{Canonicalize(multipleChoiceAnswers, v => v?.Trim().ToUpperInvariant() ?? string.Empty)}";

            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint));

            return Convert.ToHexString(hashBytes);
        }

        private static string Canonicalize<TValue>(
            IEnumerable<KeyValuePair<string, TValue>> answers,
            Func<TValue?, string> formatValue)
        {
            if (answers is null || !answers.Any())
            {
                return "none";
            }

            return string.Join(",", answers
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => $"{kv.Key.Trim()}={formatValue(kv.Value)}"));
        }
    }
}
