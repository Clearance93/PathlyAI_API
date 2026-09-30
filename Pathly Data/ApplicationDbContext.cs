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

        public DbSet<UniversityQualification> UniversityQualifications { get; set; }

        public DbSet<AiResponse> AiResponse { get; set; }

        public DbSet<SubjectResults> SubjectResults { get; set; }

        public DbSet<ImprovementAdvice> ImprovementAdvices { get; set; }

        public DbSet<EmploymentOutlook> EmploymentOutlooks { get; set; }

        public DbSet<DyingCareerWarning> DyingCareerWarning { get; set; }

        public DbSet<DemandingCareerAssessment> DemandingCareerAssessments { get; set; }

        public DbSet<CareerMatch> CareerMatches { get; set; }

        public DbSet<ApsAnalysis> ApsAnalyses { get; set; }

        public DbSet<ExtractedAcademicRecord> ExtractedAcademicRecords { get; set; }

        public DbSet<AcademicPeriod> AcademicPeriods { get; set; }

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

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // A learner owns many psychometric profiles (one per distinct score set) � removing
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

            // Refresh tokens are hashed, rotated and bound to the account. Removing the account
            // revokes them (cascade). Configured VIA the navigation property so EF does not also
            // create a duplicate shadow FK column for it.
            modelBuilder.Entity<RefreshToken>()
                .HasOne(t => t.ApplicationUser)
                .WithMany()
                .HasForeignKey(t => t.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // SHA-256 hex is 64 chars — bounded so the unique index is small and portable.
            modelBuilder.Entity<RefreshToken>()
                .Property(t => t.TokenHash)
                .HasMaxLength(64)
                .IsRequired();

            modelBuilder.Entity<RefreshToken>()
                .Property(t => t.ReplacedByTokenHash)
                .HasMaxLength(64);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(t => t.TokenHash)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(t => t.ApplicationUserId);

            // Extracted records may (optionally) be bound to the uploading account. Deleting an
            // account keeps the academic record row (SetNull) so a learner's analysis history is
            // not silently destroyed when the account goes away.
            modelBuilder.Entity<ExtractedAcademicRecord>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(r => r.ApplicationUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // An analysis result belongs to exactly one account. Deleting the account keeps the
            // row (SetNull) so aggregate/reporting data is not destroyed, but every read path
            // filters on ApplicationUserId — a logged-in learner can only ever see their own.
            modelBuilder.Entity<AiResponse>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(r => r.ApplicationUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AiResponse>()
                .HasIndex(r => new { r.ApplicationUserId, r.AddedAt });

            // Academic periods are children of their record; their subject children are removed
            // with the period. Period rows are the persisted term history of an uploaded report.
            modelBuilder.Entity<AcademicPeriod>()
                .HasOne(p => p.ExtractedAcademicRecord)
                .WithMany(r => r.AcademicPeriods)
                .HasForeignKey(p => p.ExtractedAcademicRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AcademicPeriod>()
                .HasIndex(p => new { p.ExtractedAcademicRecordId, p.Ordinal });

            // Term-block subjects live under their AcademicPeriod. Period-owned subjects carry
            // TermOrdinal/TermLabel/IsFinal so the driver/period metadata is lossless per subject.
            modelBuilder.Entity<AcademicPeriod>()
                .HasMany(p => p.Subjects)
                .WithOne(s => s.AcademicPeriod)
                .HasForeignKey(s => s.AcademicPeriodId)
                .OnDelete(DeleteBehavior.Cascade);

            // Legacy direct record->subjects relationship is preserved by convention via the
            // existing shadow FK (ExtractedAcademicRecordExtractionAcademicRecordId). New
            // multi-term records persist their subjects under AcademicPeriods instead.
        }
    }
}
