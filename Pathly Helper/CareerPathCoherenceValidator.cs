using Pathly_DTOs;

namespace Pathly_Helper
{
    public sealed class CoherenceValidationResult
    {
        /// <summary>True when the validator rewrote part of the report to make it coherent.</summary>
        public bool Repaired { get; private set; }

        /// <summary>True when the report could not be trusted and a human should check it.</summary>
        public bool NeedsManualReview { get; private set; }

        /// <summary>Human-readable explanations of what was changed or why the report was flagged.</summary>
        public List<string> Warnings { get; } = new();

        internal void MarkRepaired() => Repaired = true;

        internal void MarkReview() => NeedsManualReview = true;
    }

    /// <summary>
    /// Deterministic post-processing guard for the LLM's career report. The prompt already asks
    /// the model for a single coherent plan (one institution, one qualification), but a model can
    /// still produce the exact failure a reviewer saw: "join the robotics club at the University
    /// of Cape Town while studying mechanical engineering at the University of Pretoria". This
    /// runs after the AI and before persistence so the learner never sees a self-contradicting
    /// pathway — it repairs the roadmap to the recommended institution, or flags the report for
    /// manual review when it cannot reconcile it.
    /// </summary>
    public static class CareerPathCoherenceValidator
    {
        public static CoherenceValidationResult ValidateAndRepair(AiResponseDto? response)
        {
            var result = new CoherenceValidationResult();

            if (response?.Top3BestCareers is null || response.Top3BestCareers.Count == 0)
            {
                return result;
            }

            var topCareer = response.Top3BestCareers[0];
            var target = SaUniversities.FindPrimary(topCareer.UniversityCourse);

            if (target is null)
            {
                // No single institution can be identified for the top programme — do not silently
                // pass a report whose anchor is unknown.
                result.Warnings.Add(
                    "Pathly could not confirm that the recommended programme names a single recognised " +
                    "South African institution. Please double-check the career pathway.");
                result.MarkReview();
                return result;
            }

            AlignRoadmap(response, target, result);
            EnsureUniversityConsidered(response, target, result);
            FlagCareerInstitutionConflicts(response, target, result);

            return result;
        }

        private static void AlignRoadmap(AiResponseDto response, string target, CoherenceValidationResult result)
        {
            if (response.ImprovementtoRoadmap is null)
            {
                return;
            }

            for (var i = 0; i < response.ImprovementtoRoadmap.Count; i++)
            {
                var step = response.ImprovementtoRoadmap[i];
                if (string.IsNullOrWhiteSpace(step))
                {
                    continue;
                }

                var conflicts = SaUniversities.FindMentions(step)
                    .Where(m => !string.Equals(m, target, StringComparison.Ordinal))
                    .ToList();

                if (conflicts.Count == 0)
                {
                    continue;
                }

                var repairedStep = step;
                foreach (var conflict in conflicts)
                {
                    repairedStep = SaUniversities.ReplaceMention(repairedStep, conflict, target);
                }

                response.ImprovementtoRoadmap[i] = repairedStep;
                result.MarkRepaired();
                result.Warnings.Add(
                    $"A roadmap step mentioned {string.Join(" and ", conflicts)} while the recommended path " +
                    $"is at {target}. The step was aligned to {target} so the plan stays coherent.");
            }
        }

        private static void EnsureUniversityConsidered(AiResponseDto response, string target, CoherenceValidationResult result)
        {
            response.UniversitiestoConsider ??= new List<string>();

            var alreadyListed = response.UniversitiestoConsider
                .Any(u => string.Equals(SaUniversities.FindPrimary(u), target, StringComparison.Ordinal));

            if (!alreadyListed)
            {
                response.UniversitiestoConsider.Insert(0, target);
                result.MarkRepaired();
            }
        }

        private static void FlagCareerInstitutionConflicts(AiResponseDto response, string target, CoherenceValidationResult result)
        {
            // The top career defines the intended path; if the qualified/university lists exclude
            // that institution while it is the recommendation, the report contradicts itself.
            var qualified = response.ApsAnalysis?.UniversitiesTheyQualifyFor;

            if (qualified is null || qualified.Count == 0)
            {
                return;
            }

            var targetIsListed = qualified
                .Any(u => string.Equals(SaUniversities.FindPrimary(u?.Name), target, StringComparison.Ordinal));

            var qualifiesAny = response.ApsAnalysis?.QualifiesForUniveisty == true;

            if (qualifiesAny && !targetIsListed)
            {
                result.Warnings.Add(
                    $"The recommended path is at {target}, which is not in the learner's qualified-university " +
                    "list. Please review the qualification advice.");
                result.MarkReview();
            }
        }
    }
}
