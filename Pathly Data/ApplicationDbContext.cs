using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pathly_Models;

namespace Pathly_Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
        {
        }

        public DbSet<UniveristyQualification> UniveristyQualifications { get; set; }

        public DbSet<AiResponse> AiResponse { get; set; }

        public DbSet<SubjectResults> SubjectResults { get; set; }

        public DbSet<ImprovementAdvice> ImprovementAdvices { get; set; }

        public DbSet<EmploymentOutlook> EmploymentOutlooks { get; set; }

        public DbSet<DyingCareerWarning> DyingCareerWarning { get; set; }

        public DbSet<DemandingCareerAssessment> DemandingCareerAssessments { get; set; }

        public DbSet<CareerMatch> CareerMaths { get; set; }

        public DbSet<ApsAnalysis> ApsAnalysiss { get; set; }

        public DbSet<ExtractedAcademicRecord> ExtractedAcademicRecords { get; set; }

        public DbSet<ExtractedSubject> ExtractedSubjects { get; set; }
    }
}
