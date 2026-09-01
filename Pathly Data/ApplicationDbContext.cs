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

        public DbSet<Subject> Subjects { get; set; }

        public DbSet<PsychometricProfile> PsychometricProfiles { get; set; }

        public DbSet<PsychometricAssessment> PsychometricAssessments { get; set; }

        public DbSet<CareerProfile> CareerProfiles { get; set; }

        public DbSet<Plan> Plans { get; set; }

        public DbSet<UserSubscription> UserSubscriptions { get; set; }

        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

        public DbSet<UsageTransaction> UsageTransactions { get; set; }

        public DbSet<CreditTransaction> CreditTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // A learner owns many psychometric profiles (one per distinct score set) — removing
            // the account must not silently delete their assessment history, so no cascade.
            modelBuilder.Entity<PsychometricProfile>()
                .HasOne(p => p.ApplicationUser)
                .WithMany()
                .HasForeignKey(p => p.ApplicationUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PsychometricAssessment>()
                .HasOne(a => a.ApplicationUser)
                .WithMany()
                .HasForeignKey(a => a.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PsychometricAssessment>()
                .HasOne(a => a.PsychometricProfile)
                .WithMany()
                .HasForeignKey(a => a.PsychometricProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Plan>()
                .HasIndex(p => p.Code)
                .IsUnique();

            modelBuilder.Entity<UserSubscription>()
                .HasIndex(s => new { s.UserId, s.Status });

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(t => t.Reference)
                .IsUnique();

            modelBuilder.Entity<UsageTransaction>()
                .HasIndex(u => new { u.UserId, u.CreatedAtUtc });

            modelBuilder.Entity<CreditTransaction>()
                .HasIndex(c => c.UserId);
        }
    }
}
