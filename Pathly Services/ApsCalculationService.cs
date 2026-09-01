using AutoMapper;
using Pathly_Core.Unit;
using Pathly_DTOs;
using PathlyInterfaces.IService;

namespace Pathly_Services
{
    public class ApsCalculationService : IApsCalculationService
    {
        private readonly IUnitOfWork _Unit;
        private readonly IMapper _Mapper;

        private static readonly string[] ExcludedFromAps =
        {
            "life orientation"
        };

        public ApsCalculationService(IUnitOfWork unit,
                                     IMapper mapper)
        {
            _Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public ApsResultDto CalculateAPS(List<ExtractedSubjectDto> subjects)
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

            result.QualificationLevel = GetQualification(result.TotalAps);

            return result;
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