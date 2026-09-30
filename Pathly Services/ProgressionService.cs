using Pathly_Core.Unit;
using Pathly_DTOs;
using Pathly_Helper;
using Pathly_Interfaces.IService;
using Pathly_Models;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Pathly Tests")]

namespace Pathly_Services
{
    /// <summary>
    /// Merges every academic record an account has ever uploaded into a single chronological
    /// series, so the learner's termly and yearly results build a progression rather than each
    /// new upload replacing the last. Pure aggregation over stored data — no LLM involvement.
    /// </summary>
    public class ProgressionService : IProgressionService
    {
        // A subject moving by at least this many marks across the series counts as a real trend
        // rather than exam-to-exam noise.
        internal const int SteadyThreshold = 3;

        private readonly IUnitOfWork _Unit;
        private readonly IApsCalculationService _Aps;

        public ProgressionService(IUnitOfWork unit, IApsCalculationService aps)
        {
            _Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _Aps = aps ?? throw new ArgumentNullException(nameof(aps));
        }

        public async Task<ProgressionDto> BuildForUserAsync(string applicationUserId)
        {
            var result = new ProgressionDto();

            if (string.IsNullOrWhiteSpace(applicationUserId))
            {
                return result;
            }

            var records = await _Unit.ExtractedAcademicRecord.GetAllWithHistoryForUserAsync(applicationUserId);

            if (records.Count == 0)
            {
                return result;
            }

            result.UploadCount = records.Count;

            var snapshots = BuildSnapshots(records);

            if (snapshots.Count == 0)
            {
                return result;
            }

            result.PeriodCount = snapshots.Count;
            result.PeriodLabels = snapshots.Values.Select(s => s.Label).ToList();

            var apsValues = snapshots.Values.Select(ComputeAps).ToList();
            result.FirstAps = apsValues.First();
            result.LatestAps = apsValues.Last();
            result.ApsChange = result.LatestAps - result.FirstAps;

            result.SubjectTrends = BuildSubjectTrends(snapshots);

            result.IsAvailable = snapshots.Count >= 2;
            result.Summary = BuildSummary(result);

            return result;
        }

        internal static SortedDictionary<int, Snapshot> BuildSnapshots(List<ExtractedAcademicRecord> records)
        {
            // Records arrive oldest-first; a later upload of the same period overwrites the earlier
            // snapshot, so a re-upload always reflects the most recent data.
            var snapshots = new SortedDictionary<int, Snapshot>();

            foreach (var record in records)
            {
                if (record.AcademicPeriods.Count > 0)
                {
                    foreach (var period in record.AcademicPeriods.Where(p => p.Subjects.Count > 0))
                    {
                        var sortKey = (record.AcademicYear ?? 0) * 100 + period.Ordinal;
                        var label = string.IsNullOrWhiteSpace(period.Label) ? $"Term {period.Ordinal}" : period.Label!;

                        snapshots[sortKey] = new Snapshot(
                            label, record.AcademicYear, period.Ordinal, period.IsFinal, record.StudyLevel, period.Subjects);
                    }
                }
                else if (record.Subjects.Count > 0)
                {
                    var sortKey = (record.AcademicYear ?? 0) * 100;
                    var label = record.AcademicYear is null ? "Overall" : $"{record.AcademicYear} Overall";

                    snapshots[sortKey] = new Snapshot(
                        label, record.AcademicYear, null, true, record.StudyLevel, record.Subjects);
                }
            }

            return snapshots;
        }

        private int ComputeAps(Snapshot snapshot)
        {
            var subjects = snapshot.Subjects
                .Select(s => new ExtractedSubjectDto
                {
                    SubjectName = s.SubjectName,
                    CanonicalSubjectName = s.CanonicalSubjectName,
                    NumericMark = s.NumericMark,
                    Symbol = s.Symbol,
                    MarkType = s.MarkType
                })
                .ToList();

            return _Aps.CalculateAPS(subjects, snapshot.IsFinal, isTertiaryOrAdult: false, snapshot.StudyLevel).TotalAps;
        }

        internal static List<SubjectTrendDto> BuildSubjectTrends(SortedDictionary<int, Snapshot> snapshots)
        {
            // canonical subject -> (sortKey -> mark)
            var bySubject = new Dictionary<string, SortedDictionary<int, SubjectTrendPointDto>>(StringComparer.Ordinal);

            foreach (var (sortKey, snapshot) in snapshots)
            {
                foreach (var subject in snapshot.Subjects)
                {
                    if (subject.NumericMark is null)
                    {
                        continue;
                    }

                    var canonical = string.IsNullOrWhiteSpace(subject.CanonicalSubjectName)
                        ? SubjectCanonicalizer.ToCanonical(subject.SubjectName)
                        : subject.CanonicalSubjectName!;

                    if (string.IsNullOrWhiteSpace(canonical) || canonical == "unknown")
                    {
                        continue;
                    }

                    if (!bySubject.TryGetValue(canonical, out var points))
                    {
                        points = new SortedDictionary<int, SubjectTrendPointDto>();
                        bySubject[canonical] = points;
                    }

                    points[sortKey] = new SubjectTrendPointDto
                    {
                        PeriodLabel = snapshot.Label,
                        AcademicYear = snapshot.Year,
                        TermOrdinal = snapshot.Ordinal,
                        Mark = subject.NumericMark.Value
                    };
                }
            }

            var trends = new List<SubjectTrendDto>();

            foreach (var (canonical, points) in bySubject.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                var ordered = points.Values.ToList();
                var subjectName = canonical;

                // Prefer the most recent human-readable name for the subject.
                var displayName = snapshots.Values
                    .SelectMany(s => s.Subjects)
                    .LastOrDefault(s => string.Equals(
                        string.IsNullOrWhiteSpace(s.CanonicalSubjectName) ? SubjectCanonicalizer.ToCanonical(s.SubjectName) : s.CanonicalSubjectName,
                        canonical,
                        StringComparison.Ordinal))?.SubjectName;

                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    subjectName = displayName!;
                }

                int? change = ordered.Count >= 2 ? ordered[^1].Mark - ordered[0].Mark : null;

                var direction = change switch
                {
                    null => "insufficient-data",
                    >= SteadyThreshold => "improving",
                    <= -SteadyThreshold => "declining",
                    _ => "steady"
                };

                trends.Add(new SubjectTrendDto
                {
                    SubjectName = subjectName,
                    CanonicalName = canonical,
                    Points = ordered,
                    ChangeSinceFirst = change,
                    Direction = direction
                });
            }

            return trends;
        }

        private static string BuildSummary(ProgressionDto result)
        {
            if (result.PeriodCount < 2)
            {
                return "Only one reporting period has been uploaded so far. Upload another term to see your progress over time.";
            }

            var parts = new List<string>
            {
                $"Across {result.UploadCount} upload{(result.UploadCount == 1 ? string.Empty : "s")} " +
                $"({result.PeriodCount} reporting periods), your APS moved from {result.FirstAps} to {result.LatestAps} " +
                $"({FormatDelta(result.ApsChange)})."
            };

            var improving = result.SubjectTrends.Where(t => t.Direction == "improving").Select(t => t.SubjectName).ToList();
            var declining = result.SubjectTrends.Where(t => t.Direction == "declining").Select(t => t.SubjectName).ToList();

            if (improving.Count > 0)
            {
                parts.Add($"Improving: {string.Join(", ", improving)}.");
            }

            if (declining.Count > 0)
            {
                parts.Add($"Needs attention: {string.Join(", ", declining)}.");
            }

            return string.Join(" ", parts);
        }

        private static string FormatDelta(int? delta) => delta switch
        {
            null => "no comparison yet",
            > 0 => $"+{delta} points",
            < 0 => $"{delta} points",
            _ => "no change"
        };

        internal sealed record Snapshot(
            string Label,
            int? Year,
            int? Ordinal,
            bool IsFinal,
            string? StudyLevel,
            IReadOnlyList<ExtractedSubject> Subjects);
    }
}
