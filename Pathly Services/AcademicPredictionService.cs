using Pathly_DTOs;
using Pathly_Helper;
using Pathly_Interfaces.IService;

namespace Pathly_Services
{
    /// <summary>
    /// Deterministic next-term projection over a learner's stored term history. Uses a damped
    /// linear trend per subject so a single bad term (e.g. a hard June exam) cannot mechanically
    /// extrapolate a collapse from two data points; results are emitted as a band plus a
    /// confidence flag, never a false-precision point estimate.
    /// </summary>
    public class AcademicPredictionService : IAcademicPredictionService
    {
        // A subject projected below this mark, or trending down sharply from an already-low
        // mark, is flagged as at-risk and may steer improvement advice.
        internal const int AtRiskMarkThreshold = 40;
        internal const int SharpDeclineThreshold = 15;
        internal const int SteadySlopeThreshold = 2;

        public AcademicPredictionDto PredictNextTerm(ExtractedAcademicRecordDto record)
        {
            var result = new AcademicPredictionDto();

            if (record is null)
            {
                return result;
            }

            // Prediction needs at least two populated same-year terms, and stops at a populated
            // Final/Promotion block (year-end → no "next term" in the same year to predict).
            var populatedTerms = record.AcademicPeriods
                .Where(p => !p.IsFinal && p.Subjects.Count > 0)
                .OrderBy(p => p.Ordinal)
                .ToList();

            var finalBlock = record.AcademicPeriods.FirstOrDefault(p => p.IsFinal && p.Subjects.Count > 0);
            if (finalBlock is not null)
            {
                return result;
            }

            var distinctOrdinals = populatedTerms
                .Select(p => p.Ordinal)
                .Distinct()
                .OrderBy(o => o)
                .ToList();

            if (distinctOrdinals.Count < 2)
            {
                return result;
            }

            // Only predict within the same academic year. A healthy two-term series is at most 3
            // ordinals apart (Term 1 → Term 4); anything wider suggests the "terms" actually span
            // years, and we do not extrapolate across years.
            if (distinctOrdinals[^1] - distinctOrdinals[0] > 3)
            {
                return result;
            }

            var maxOrdinal = distinctOrdinals[^1];

            // Group each term's marks by canonical subject so "(Gr 10)"/alias variants and
            // repeated subjects line up into a single per-subject, per-term series.
            var pointsByCanonical = new Dictionary<string, SortedDictionary<int, int>>(StringComparer.Ordinal);

            foreach (var term in populatedTerms)
            {
                foreach (var subject in term.Subjects)
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

                    if (!pointsByCanonical.TryGetValue(canonical, out var byOrdinal))
                    {
                        byOrdinal = new SortedDictionary<int, int>();
                        pointsByCanonical[canonical] = byOrdinal;
                    }

                    // One mark per (canonical subject, term) — the highest-ordinal term wins if a
                    // subject somehow repeats inside the same term block.
                    byOrdinal[term.Ordinal] = subject.NumericMark.Value;
                }
            }

            // A series is only usable if it has marks in at least two distinct reported terms,
            // including the most recent one (so the projection extends from the latest standing).
            foreach (var (canonical, byOrdinal) in pointsByCanonical.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                if (byOrdinal.Count < 2 || !byOrdinal.ContainsKey(maxOrdinal))
                {
                    continue;
                }

                var points = byOrdinal.Values.ToList();
                var projection = ProjectSubject(canonical, points, byOrdinal.Keys.ToList());
                if (projection is null)
                {
                    continue;
                }

                result.SubjectPredictions.Add(projection);

                var latest = projection.LatestMark;
                if (latest < AtRiskMarkThreshold ||
                    (latest >= AtRiskMarkThreshold && projection.Direction == "down" && projection.ProjectedHigh < AtRiskMarkThreshold) ||
                    (latest - projection.ProjectedHigh >= SharpDeclineThreshold))
                {
                    result.AtRiskSubjects.Add(canonical);
                }
            }

            if (result.SubjectPredictions.Count == 0)
            {
                return result;
            }

            result.IsPredictionAvailable = true;
            result.NextTermOrdinal = maxOrdinal + 1;
            result.NextTermLabel = $"Term {maxOrdinal + 1}";
            result.Caveat =
                "Projections are estimates based on the trend in the terms uploaded so far. " +
                "A single difficult term can pull a straight-line estimate down sharply, so treat " +
                "these as a direction and range to watch, not a guaranteed mark.";

            return result;
        }

        private static SubjectPredictionDto? ProjectSubject(
            string canonical,
            IReadOnlyList<int> marks,
            IReadOnlyList<int> ordinals)
        {
            if (marks.Count < 2)
            {
                return null;
            }

            var latest = marks[^1];
            var slope = ComputeDampedSlope(ordinals, marks);
            var direction = Math.Abs(slope) <= SteadySlopeThreshold
                ? "steady"
                : slope > 0 ? "up" : "down";

            // Two-point series are real but noisy — widen the band and flag low confidence so
            // nobody mistakes a projection for a guarantee (Afrikaans 58 -> 28 stays a band, not a
            // false "22%").
            var lowConfidence = marks.Count <= 2;
            var spread = lowConfidence ? 18 : 12;

            var projected = latest + slope;
            var low = Math.Clamp((int)Math.Round(projected - spread / 2.0), 0, 100);
            var high = Math.Clamp((int)Math.Round(projected + spread / 2.0), 0, 100);

            return new SubjectPredictionDto
            {
                SubjectName = canonical,
                LatestMark = latest,
                ProjectedLow = low,
                ProjectedHigh = high,
                Direction = direction,
                LowConfidence = lowConfidence
            };
        }

        /// <summary>
        /// Least-squares slope over the available (term ordinal, mark) points, damped toward the
        /// learner's own mean so one catastrophic term cannot extrapolate a collapse indefinitely.
        /// With two points the slope is the raw per-term difference halved; with more it is the
        /// ordinary least-squares slope, then shrunk by a damping factor.
        /// </summary>
        private static double ComputeDampedSlope(IReadOnlyList<int> ordinals, IReadOnlyList<int> marks)
        {
            var n = marks.Count;
            if (n < 2)
            {
                return 0;
            }

            double xMean = ordinals.Average();
            double yMean = marks.Average();

            double numerator = 0;
            double denominator = 0;
            for (var i = 0; i < n; i++)
            {
                var x = ordinals[i] - xMean;
                numerator += x * (marks[i] - yMean);
                denominator += x * x;
            }

            var rawSlope = denominator == 0 ? 0 : numerator / denominator;

            // Shrink the slope toward zero the fewer points we have. With two points this halves
            // the naive difference; with more it approaches the raw least-squares slope.
            var damping = n <= 2 ? 0.5 : n <= 3 ? 0.65 : 0.8;

            return rawSlope * damping;
        }
    }
}
