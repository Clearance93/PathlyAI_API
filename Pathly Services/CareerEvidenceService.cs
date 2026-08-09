using Microsoft.Extensions.Options;
using Pathly_Core.Unit;
using Pathly_DTOs;
using Pathly_Helper;
using Pathly_Models;
using PathlyInterfaces.IService;

namespace Pathly_Services
{
    /// <summary>
    /// Computes career evidence deterministically from Pathly's own career knowledge base
    /// (Part 9/10) BEFORE any AI call. The AI is then asked to explain this evidence rather
    /// than invent career facts, demand levels, or growth outlooks (Part 11).
    ///
    /// Distinguishes four evidence dimensions plus an optional fifth (psychometric fit), with
    /// configurable relative weights (<see cref="CareerMatchWeightsOptions"/>) rather than
    /// scattered hardcoded scoring logic.
    /// </summary>
    public class CareerEvidenceService : ICareerEvidenceService
    {
        private readonly IUnitOfWork _Unit;
        private readonly CareerMatchWeightsOptions _Weights;

        public CareerEvidenceService(IUnitOfWork unit, IOptions<CareerMatchWeightsOptions> weights)
        {
            _Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _Weights = weights?.Value ?? new CareerMatchWeightsOptions();
        }

        public async Task<List<CareerEvidenceDto>> ComputeEvidenceAsync(
            ExtractedAcademicRecordDto academicRecord,
            ApsResultDto apsResult,
            PsychometricProfileDto? psychometricProfile = null,
            int topN = 10)
        {
            var careers = await _Unit.CareerProfile.GetAllCareersAsync();

            if (careers.Count == 0)
            {
                // No seeded career knowledge base yet — nothing to compute evidence against.
                // Callers should treat an empty list as "let the AI use its own general
                // knowledge", not as an error.
                return new List<CareerEvidenceDto>();
            }

            var learnerSubjects = academicRecord.Subjects
                .Where(s => !string.IsNullOrWhiteSpace(s.SubjectName))
                .ToDictionary(
                    s => SubjectNormalizer.Normalize(s.SubjectName),
                    s => s.NumericMark,
                    StringComparer.Ordinal);

            var evidence = careers
                .Select(career => BuildEvidence(career, apsResult, learnerSubjects, psychometricProfile))
                .OrderByDescending(e => e.OverallScore)
                .Take(topN)
                .ToList();

            return evidence;
        }

        private CareerEvidenceDto BuildEvidence(
            CareerProfile career,
            ApsResultDto apsResult,
            IReadOnlyDictionary<string, int?> learnerSubjects,
            PsychometricProfileDto? psychometricProfile)
        {
            var academicFit = ComputeAcademicFit(apsResult.TotalAps, career.MinimumAps);
            var subjectAlignment = ComputeSubjectAlignment(career.RequiredSubjects, learnerSubjects);
            var psychometricFit = psychometricProfile is null
                ? (int?)null
                : ComputePsychometricFit(career, psychometricProfile);

            var overallScore = ComputeOverallScore(academicFit, subjectAlignment, psychometricFit, career.DemandScore, career.GrowthScore);

            return new CareerEvidenceDto
            {
                CareerName = career.CareerName,
                Category = career.Category,
                AcademicFit = academicFit,
                SubjectAlignment = subjectAlignment,
                PsychometricFit = psychometricFit,
                CareerDemand = career.DemandScore,
                FutureGrowth = career.GrowthScore,
                OverallScore = overallScore,
                Description = career.Description
            };
        }

        private static int ComputeAcademicFit(int totalAps, int minimumAps)
        {
            if (minimumAps <= 0)
            {
                return 50; // No academic bar defined for this career — treat as neutral.
            }

            if (totalAps >= minimumAps)
            {
                // Meets the bar: 70 baseline, plus headroom above it (capped at 100).
                var headroom = Math.Min(30, (totalAps - minimumAps) * 2);
                return Math.Clamp(70 + headroom, 0, 100);
            }

            // Below the bar: partial, proportional credit rather than a hard zero.
            var proportional = (double)totalAps / minimumAps * 70;
            return Math.Clamp((int)Math.Round(proportional), 0, 100);
        }

        private static int ComputeSubjectAlignment(string requiredSubjectsCsv, IReadOnlyDictionary<string, int?> learnerSubjects)
        {
            var requiredSubjects = (requiredSubjectsCsv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(SubjectNormalizer.Normalize)
                .Distinct()
                .ToList();

            if (requiredSubjects.Count == 0)
            {
                return 50; // No specific subject requirements defined — neutral score.
            }

            var scores = requiredSubjects.Select(required =>
                learnerSubjects.TryGetValue(required, out var mark) && mark.HasValue
                    ? Math.Clamp(mark.Value, 0, 100)
                    : 0);

            return (int)Math.Round(scores.Average());
        }

        private static int ComputePsychometricFit(CareerProfile career, PsychometricProfileDto profile)
        {
            var weightSum =
                career.RealisticWeight + career.InvestigativeWeight + career.ArtisticWeight +
                career.SocialWeight + career.EnterprisingWeight + career.ConventionalWeight;

            if (weightSum <= 0)
            {
                return 50; // Career has no defined RIASEC profile — neutral score.
            }

            var weightedScore =
                (profile.Realistic * career.RealisticWeight) +
                (profile.Investigative * career.InvestigativeWeight) +
                (profile.Artistic * career.ArtisticWeight) +
                (profile.Social * career.SocialWeight) +
                (profile.Enterprising * career.EnterprisingWeight) +
                (profile.Conventional * career.ConventionalWeight);

            // Both scores and weights are 0-100, so the max possible weightedScore is 100 * weightSum.
            var normalized = weightedScore / (100.0 * weightSum) * 100.0;

            return Math.Clamp((int)Math.Round(normalized), 0, 100);
        }

        private double ComputeOverallScore(int academicFit, int subjectAlignment, int? psychometricFit, int careerDemand, int futureGrowth)
        {
            var weightedTotal = (academicFit * _Weights.AcademicFit)
                + (subjectAlignment * _Weights.SubjectAlignment)
                + (careerDemand * _Weights.CareerDemand)
                + (futureGrowth * _Weights.FutureGrowth);

            var weightSum = _Weights.AcademicFit + _Weights.SubjectAlignment + _Weights.CareerDemand + _Weights.FutureGrowth;

            if (psychometricFit.HasValue)
            {
                weightedTotal += psychometricFit.Value * _Weights.PsychometricFit;
                weightSum += _Weights.PsychometricFit;
            }

            return weightSum <= 0 ? 0 : Math.Round(weightedTotal / weightSum, 1);
        }
    }
}
