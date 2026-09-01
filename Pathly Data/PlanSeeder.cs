using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pathly_Enums;
using Pathly_Models;

namespace Pathly_Data
{
    /// <summary>
    /// Idempotently seeds the sellable plan catalogue. Existing rows are updated in place so
    /// price/quota changes ship with deployments without manual database edits.
    /// </summary>
    public static class PlanSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
        {
            var catalog = new List<Plan>
            {
                new()
                {
                    Code = "free", Name = "Free", Audience = PlanAudience.Individual,
                    Description = "Upload results, APS calculation, basic career matches.",
                    Interval = PlanInterval.OneOff, PriceInCents = 0,
                    MonthlyAnalysisQuota = 5, MonthlyPsychometricQuota = 2, IncludesPremiumAnalysis = false,
                    DisplayOrder = 0
                },
                new()
                {
                    Code = "career_report", Name = "Career Report", Audience = PlanAudience.Individual,
                    Description = "Full academic career report: matches, universities, subject requirements, roadmap.",
                    Interval = PlanInterval.OneOff, PriceInCents = 12900,
                    MonthlyAnalysisQuota = 10, MonthlyPsychometricQuota = 2, IncludesPremiumAnalysis = false,
                    DisplayOrder = 1
                },
                new()
                {
                    Code = "psychometric_report", Name = "Psychometric Report", Audience = PlanAudience.Individual,
                    Description = "RIASEC assessment combined with your academics for deeper matching.",
                    Interval = PlanInterval.OneOff, PriceInCents = 34900,
                    MonthlyAnalysisQuota = 5, MonthlyPsychometricQuota = 10, IncludesPremiumAnalysis = true,
                    DisplayOrder = 2
                },
                new()
                {
                    Code = "pro_monthly", Name = "Pro", Audience = PlanAudience.Individual,
                    Description = "Unlimited analyses, AI assistant, bursaries and application planning.",
                    Interval = PlanInterval.Monthly, PriceInCents = 9900,
                    MonthlyAnalysisQuota = null, MonthlyPsychometricQuota = null, IncludesPremiumAnalysis = true,
                    DisplayOrder = 3
                },
                new()
                {
                    Code = "student_monthly", Name = "Student", Audience = PlanAudience.Student,
                    Description = "Degree-to-career mapping, skills gap analysis and internship guidance.",
                    Interval = PlanInterval.Monthly, PriceInCents = 4900,
                    MonthlyAnalysisQuota = 15, MonthlyPsychometricQuota = 5, IncludesPremiumAnalysis = true,
                    DisplayOrder = 4
                },
                new()
                {
                    Code = "professional_monthly", Name = "Professional", Audience = PlanAudience.Professional,
                    Description = "For psychologists and career counsellors: client assessments, reports and tracking.",
                    Interval = PlanInterval.Monthly, PriceInCents = 49900,
                    MonthlyAnalysisQuota = null, MonthlyPsychometricQuota = null, IncludesPremiumAnalysis = true,
                    DisplayOrder = 5
                },
                new()
                {
                    Code = "school_starter", Name = "School Starter", Audience = PlanAudience.Organization,
                    Description = "Up to 100 learners/year: analyses, matching, dashboards and reports.",
                    Interval = PlanInterval.Annually, PriceInCents = 500000,
                    MonthlyAnalysisQuota = null, MonthlyPsychometricQuota = 200, IncludesPremiumAnalysis = false,
                    DisplayOrder = 6
                },
                new()
                {
                    Code = "school_growth", Name = "School Growth", Audience = PlanAudience.Organization,
                    Description = "Up to 300 learners/year with grade-level analytics and parent reports.",
                    Interval = PlanInterval.Annually, PriceInCents = 1000000,
                    MonthlyAnalysisQuota = null, MonthlyPsychometricQuota = 600, IncludesPremiumAnalysis = true,
                    DisplayOrder = 7
                },
                new()
                {
                    Code = "school_pro", Name = "School Pro", Audience = PlanAudience.Organization,
                    Description = "Up to 500 learners/year: full psychometrics, counsellor tools, priority support.",
                    Interval = PlanInterval.Annually, PriceInCents = 2000000,
                    MonthlyAnalysisQuota = null, MonthlyPsychometricQuota = null, IncludesPremiumAnalysis = true,
                    DisplayOrder = 8
                }
            };

            var existing = await context.Plans.ToDictionaryAsync(p => p.Code);

            foreach (var seed in catalog)
            {
                if (existing.TryGetValue(seed.Code, out var current))
                {
                    current.Name = seed.Name;
                    current.Description = seed.Description;
                    current.Audience = seed.Audience;
                    current.Interval = seed.Interval;
                    current.PriceInCents = seed.PriceInCents;
                    current.Currency = seed.Currency;
                    current.MonthlyAnalysisQuota = seed.MonthlyAnalysisQuota;
                    current.MonthlyPsychometricQuota = seed.MonthlyPsychometricQuota;
                    current.IncludesPremiumAnalysis = seed.IncludesPremiumAnalysis;
                    current.DisplayOrder = seed.DisplayOrder;
                    current.IsActive = true;

                    context.Plans.Update(current);
                }
                else
                {
                    seed.PlanId = Guid.NewGuid();

                    await context.Plans.AddAsync(seed);
                }
            }

            await context.SaveChangesAsync();

            logger.LogInformation("Plan catalogue seeded ({Count} plans).", catalog.Count);
        }
    }
}
