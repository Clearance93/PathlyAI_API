using Pathly_DTOs;
using Pathly_Helper;

namespace Pathly_Services
{
    /// <summary>
    /// Keeps the learner-facing subject marks and achievement levels tied to the validated
    /// extraction instead of allowing generated report text to change the source results.
    /// </summary>
    public static class SubjectResultsReconciler
    {
        public static void Reconcile(
            AiResponseDto response,
            IEnumerable<ExtractedSubjectDto> extractedSubjects,
            IEnumerable<SubjectApsDto>? apsSubjects = null)
        {
            ArgumentNullException.ThrowIfNull(response);
            ArgumentNullException.ThrowIfNull(extractedSubjects);

            var generated = (response.SubjectResults ?? [])
                .GroupBy(s => SubjectCanonicalizer.ToCanonical(s.Subject))
                .ToDictionary(g => g.Key, g => g.First());
            var apsBySubject = (apsSubjects ?? [])
                .GroupBy(s => SubjectCanonicalizer.ToCanonical(s.SubjectName))
                .ToDictionary(g => g.Key, g => g.First());

            response.SubjectResults = extractedSubjects
                .Where(s => !string.IsNullOrWhiteSpace(s.SubjectName))
                .Select(subject =>
                {
                    var name = subject.SubjectName!.Trim();
                    var canonicalName = string.IsNullOrWhiteSpace(subject.CanonicalSubjectName)
                        ? SubjectCanonicalizer.ToCanonical(name)
                        : subject.CanonicalSubjectName;
                    generated.TryGetValue(canonicalName, out var generatedResult);
                    apsBySubject.TryGetValue(canonicalName, out var apsSubject);

                    var mark = subject.NumericMark ?? 0;
                    return new SubjectResultsDto
                    {
                        Subject = name,
                        Mark = mark,
                        ApsPoints = apsSubject?.ApsPoints ?? ApsPointsForMark(mark),
                        IncludedInApsCalculation = apsSubject?.IncludedInCalculation ?? true,
                        Grade = $"Level {ApsPointsForMark(mark)}",
                        CareerRelevance = generatedResult?.CareerRelevance,
                        ImprovementTip = generatedResult?.ImprovementTip
                    };
                })
                .ToList();
        }

        public static int ApsPointsForMark(int mark)
        {
            if (mark >= 80) return 7;
            if (mark >= 70) return 6;
            if (mark >= 60) return 5;
            if (mark >= 50) return 4;
            if (mark >= 40) return 3;
            if (mark >= 30) return 2;
            return 1;
        }
    }
}
