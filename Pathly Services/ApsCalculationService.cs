using Pathly_DTOs;
using Pathly_Helper;
using Pathly_Interfaces.IService;

namespace Pathly_Services
{
    public class ApsCalculationService : IApsCalculationService
    {
        private static readonly string[] ExcludedFromAps =
        {
            "life orientation"
        };

        public ApsCalculationService()
        {
        }

        public ApsResultDto CalculateAPS(List<ExtractedSubjectDto> subjects)
        {
            // Preserve legacy single-document behaviour: a flat subject list is treated as a
            // final/overall snapshot (no qualification gating).
            return CalculateAPS(subjects, isFinal: true, isTertiaryOrAdult: false, studyLevel: null);
        }

        public ApsResultDto CalculateAPS(List<ExtractedSubjectDto> subjects, bool isFinal, bool isTertiaryOrAdult, string? studyLevel)
        {
            if (subjects == null)
            {
                throw new ArgumentNullException(nameof(subjects));
            }

            var result = new ApsResultDto();

            foreach (var subject in subjects)
            {
                var isExcluded = IsExcludedFromAps(subject.SubjectName);

                var apsPoints = ConvertMarkToAPS(subject.NumericMark ?? 0);

                var subjectAps = new SubjectApsDto
                {
                    SubjectName = subject.SubjectName!,
                    Percentage = subject.NumericMark ?? 0,
                    ApsPoints = apsPoints,
                    IncludedInCalculation = !isExcluded
                };

                result.Subjects!.Add(subjectAps);

                if (!isExcluded)
                {
                    result.TotalAps += apsPoints;
                }
            }

            result.AverageMark = subjects.Count == 0 ? 0 : subjects.Average(x => x.NumericMark ?? 0);

            result.Distinctions = subjects.Count(x => (x.NumericMark ?? 0) >= 80);

            // A non-final (mid-year) or non-final-year driver is "current standing — indicative",
            // never an administrative qualification verdict. Only a final block on a final-year
            // (Gr 12 NSC) report may drive hard qualification language.
            result.QualificationLevel = isFinal && !isTertiaryOrAdult && IsFinalYearStudyLevel(studyLevel)
                ? GetQualification(result.TotalAps)
                : GetIndicativeStanding(result.TotalAps);

            return result;
        }

        private static bool IsFinalYearStudyLevel(string? studyLevel)
        {
            if (string.IsNullOrWhiteSpace(studyLevel))
            {
                return false;
            }

            // "Grade 12", "Gr 12", "Matric", "NSC" => final-year secondary.
            var level = SubjectNormalizer.Normalize(studyLevel);
            return level == "grade 12"
                || level == "gr 12"
                || level == "matric"
                || level == "nsc"
                || level.StartsWith("grade 12", StringComparison.Ordinal)
                || level.StartsWith("gr 12", StringComparison.Ordinal);
        }

        private static bool IsExcludedFromAps(string? subjectName)
        {
            if (string.IsNullOrWhiteSpace(subjectName))
            {
                return false;
            }

            return ExcludedFromAps.Any(excluded =>
                subjectName.Contains(excluded, StringComparison.OrdinalIgnoreCase));
        }

        private string GetQualification(int aps)
        {
            if (aps >= 42)
                return "Excellent University Admission";

            if (aps >= 38)
                return "Competitive University Admission";

            if (aps >= 30)
                return "University Bachelor's Pass";

            if (aps >= 24)
                return "Diploma Pass";

            if (aps >= 18)
                return "Higher Certificate Pass";

            return "Does not currently qualify for university";
        }

        /// <summary>
        /// Mid-year/non-final-year standing. The numeric APS is still meaningful as a current
        /// snapshot, but the wording deliberately avoids an administrative qualification verdict.
        /// </summary>
        private string GetIndicativeStanding(int aps)
        {
            if (aps >= 42)
                return "Indicative standing: tracking toward Excellent University Admission (final year results will confirm)";

            if (aps >= 38)
                return "Indicative standing: tracking toward Competitive University Admission (final year results will confirm)";

            if (aps >= 30)
                return "Indicative standing: tracking toward a University Bachelor's Pass (final year results will confirm)";

            if (aps >= 24)
                return "Indicative standing: tracking toward a Diploma Pass (final year results will confirm)";

            if (aps >= 18)
                return "Indicative standing: tracking toward a Higher Certificate Pass (final year results will confirm)";

            return "Indicative current standing only — this is a mid-year/non-final snapshot and is not an admission verdict. The final year-end results determine university eligibility.";
        }

        private int ConvertMarkToAPS(int mark)
        {
            if (mark >= 80)
            {
                return 7;
            }
            else if (mark >= 70)
            {
                return 6;
            }
            else if (mark >= 60)
            {
                return 5;
            }
            else if (mark >= 50)
            {
                return 4;
            }
            else if (mark >= 40)
            {
                return 3;
            }
            else if (mark >= 30)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }

        public string GetApsExplanation(int aps)
        {
            if (aps >= 30) return $"APS {aps} — Qualifies for most university programmes.";
            if (aps >= 20) return $"APS {aps} — Qualifies for some diploma and certificate programmes.";

            return $"APS {aps} — May need to consider bridging courses or upgrading subjects.";
        }
    }
}
