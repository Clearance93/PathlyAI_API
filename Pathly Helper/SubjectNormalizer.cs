namespace Pathly_Helper
{
    /// <summary>
    /// Single source of truth for turning a raw string (subject name, study level, mark type,
    /// etc.) into a normalized token. Used by both <see cref="AcademicRecordFingerprint"/> (so
    /// cache keys are stable) and subject persistence (so "Mathematics", " mathematics ", and
    /// "MATHEMATICS" all resolve to the same canonical subject record).
    /// </summary>
    public static class SubjectNormalizer
    {
        public static string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "unknown"
                : string.Join(' ', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
