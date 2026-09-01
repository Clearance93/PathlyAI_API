using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Pathly_Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicRecords",
                columns: table => new
                {
                    AcadmicRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<string>(type: "text", nullable: true),
                    StudentName = table.Column<string>(type: "text", nullable: true),
                    Grade = table.Column<string>(type: "text", nullable: true),
                    ClalculatedAPS = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicRecords", x => x.AcadmicRecordId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    Password = table.Column<string>(type: "text", nullable: true),
                    ProofilePictures = table.Column<string>(type: "text", nullable: true),
                    AuthProvider = table.Column<int>(type: "integer", nullable: false),
                    GoogleId = table.Column<string>(type: "text", nullable: true),
                    MicrosoftId = table.Column<string>(type: "text", nullable: true),
                    Subscription = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CareerProfiles",
                columns: table => new
                {
                    CareerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequiredSubjects = table.Column<string>(type: "text", nullable: false),
                    MinimumAps = table.Column<int>(type: "integer", nullable: false),
                    RealisticWeight = table.Column<int>(type: "integer", nullable: false),
                    InvestigativeWeight = table.Column<int>(type: "integer", nullable: false),
                    ArtisticWeight = table.Column<int>(type: "integer", nullable: false),
                    SocialWeight = table.Column<int>(type: "integer", nullable: false),
                    EnterprisingWeight = table.Column<int>(type: "integer", nullable: false),
                    ConventionalWeight = table.Column<int>(type: "integer", nullable: false),
                    DemandScore = table.Column<int>(type: "integer", nullable: false),
                    GrowthScore = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerProfiles", x => x.CareerProfileId);
                });

            migrationBuilder.CreateTable(
                name: "ExtractedAcademicRecords",
                columns: table => new
                {
                    ExtractionAcademicRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentName = table.Column<string>(type: "text", nullable: true),
                    InstitutionName = table.Column<string>(type: "text", nullable: true),
                    InstitutionType = table.Column<string>(type: "text", nullable: true),
                    StudyLevel = table.Column<string>(type: "text", nullable: true),
                    RawExtractedText = table.Column<string>(type: "text", nullable: true),
                    ExtractedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractedAcademicRecords", x => x.ExtractionAcademicRecordId);
                });

            migrationBuilder.CreateTable(
                name: "ImprovementAdvices",
                columns: table => new
                {
                    ImprovementAdviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShouldReWriteMatric = table.Column<bool>(type: "boolean", nullable: false),
                    ShouldUpgradeSubjects = table.Column<bool>(type: "boolean", nullable: false),
                    RecommendedSubjecrsToImprove = table.Column<List<string>>(type: "text[]", nullable: true),
                    AlternativeOptions = table.Column<List<string>>(type: "text[]", nullable: true),
                    MotivationalGuidance = table.Column<string>(type: "text", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImprovementAdvices", x => x.ImprovementAdviceId);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Audience = table.Column<int>(type: "integer", nullable: false),
                    Interval = table.Column<int>(type: "integer", nullable: false),
                    PriceInCents = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    MonthlyAnalysisQuota = table.Column<int>(type: "integer", nullable: true),
                    MonthlyPsychometricQuota = table.Column<int>(type: "integer", nullable: true),
                    IncludesPremiumAnalysis = table.Column<bool>(type: "boolean", nullable: false),
                    ProviderPlanCode = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.PlanId);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.SubjectId);
                });

            migrationBuilder.CreateTable(
                name: "UniveristyQualifications",
                columns: table => new
                {
                    UnviversityQualificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    MinimumAPS = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: true),
                    RecommendedCourse = table.Column<List<string>>(type: "text[]", nullable: true),
                    Gap = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniveristyQualifications", x => x.UnviversityQualificationId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditTransactions",
                columns: table => new
                {
                    CreditTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: true),
                    Delta = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditTransactions", x => x.CreditTransactionId);
                    table.ForeignKey(
                        name: "FK_CreditTransactions_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PsychometricProfiles",
                columns: table => new
                {
                    PsychometricProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: true),
                    Realistic = table.Column<int>(type: "integer", nullable: false),
                    Investigative = table.Column<int>(type: "integer", nullable: false),
                    Artistic = table.Column<int>(type: "integer", nullable: false),
                    Social = table.Column<int>(type: "integer", nullable: false),
                    Enterprising = table.Column<int>(type: "integer", nullable: false),
                    Conventional = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsychometricProfiles", x => x.PsychometricProfileId);
                    table.ForeignKey(
                        name: "FK_PsychometricProfiles_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UsageTransactions",
                columns: table => new
                {
                    UsageTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: true),
                    UsageType = table.Column<int>(type: "integer", nullable: false),
                    Units = table.Column<int>(type: "integer", nullable: false),
                    EstimatedCostInCents = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageTransactions", x => x.UsageTransactionId);
                    table.ForeignKey(
                        name: "FK_UsageTransactions_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExtractedSubjects",
                columns: table => new
                {
                    ExtractionSubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectName = table.Column<string>(type: "text", nullable: true),
                    RawMark = table.Column<string>(type: "text", nullable: true),
                    NumericMark = table.Column<int>(type: "integer", nullable: true),
                    Symbol = table.Column<string>(type: "text", nullable: true),
                    MarkType = table.Column<string>(type: "text", nullable: true),
                    ExtractedAcademicRecordExtractionAcademicRecordId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractedSubjects", x => x.ExtractionSubjectId);
                    table.ForeignKey(
                        name: "FK_ExtractedSubjects_ExtractedAcademicRecords_ExtractedAcademi~",
                        column: x => x.ExtractedAcademicRecordExtractionAcademicRecordId,
                        principalTable: "ExtractedAcademicRecords",
                        principalColumn: "ExtractionAcademicRecordId");
                });

            migrationBuilder.CreateTable(
                name: "ApsAnalysiss",
                columns: table => new
                {
                    ApsAnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculatedAps = table.Column<int>(type: "integer", nullable: false),
                    ApsExplanation = table.Column<string>(type: "text", nullable: true),
                    QualifiesForUniversity = table.Column<bool>(type: "boolean", nullable: false),
                    QualificationMessage = table.Column<string>(type: "text", nullable: true),
                    UniversitiesTheyQualifyFor = table.Column<List<string>>(type: "text[]", nullable: true),
                    UniversitiestheyDoNotQualifyFor = table.Column<List<string>>(type: "text[]", nullable: true),
                    ImprovementAdviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImprovementId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApsAnalysiss", x => x.ApsAnalysisId);
                    table.ForeignKey(
                        name: "FK_ApsAnalysiss_ImprovementAdvices_ImprovementAdviceId",
                        column: x => x.ImprovementAdviceId,
                        principalTable: "ImprovementAdvices",
                        principalColumn: "ImprovementAdviceId");
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: true),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    AmountInCents = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    ProviderTransactionRef = table.Column<string>(type: "text", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.PaymentTransactionId);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "PlanId");
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    UserSubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false),
                    ProviderSubscriptionRef = table.Column<string>(type: "text", nullable: true),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.UserSubscriptionId);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PsychometricAssessments",
                columns: table => new
                {
                    PsychometricAssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    PsychometricProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    RatingAnswersJson = table.Column<string>(type: "text", nullable: true),
                    TrueFalseAnswersJson = table.Column<string>(type: "text", nullable: true),
                    MultipleChoiceAnswersJson = table.Column<string>(type: "text", nullable: true),
                    TotalQuestions = table.Column<int>(type: "integer", nullable: false),
                    AnsweredQuestions = table.Column<int>(type: "integer", nullable: false),
                    ResultFingerprint = table.Column<string>(type: "text", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsychometricAssessments", x => x.PsychometricAssessmentId);
                    table.ForeignKey(
                        name: "FK_PsychometricAssessments_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PsychometricAssessments_PsychometricProfiles_PsychometricPr~",
                        column: x => x.PsychometricProfileId,
                        principalTable: "PsychometricProfiles",
                        principalColumn: "PsychometricProfileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiResponse",
                columns: table => new
                {
                    AiResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserFullName = table.Column<string>(type: "text", nullable: true),
                    Grade = table.Column<string>(type: "text", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    ApsAnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseJson = table.Column<string>(type: "text", nullable: true),
                    SubjectSetHash = table.Column<string>(type: "text", nullable: true),
                    PsychometricHash = table.Column<string>(type: "text", nullable: true),
                    AnalysisVersion = table.Column<string>(type: "text", nullable: true),
                    PromptVersion = table.Column<string>(type: "text", nullable: true),
                    IsPremium = table.Column<bool>(type: "boolean", nullable: false),
                    OverallScore = table.Column<double>(type: "double precision", nullable: false),
                    AcademicPersonality = table.Column<string>(type: "text", nullable: true),
                    FeedBack = table.Column<string>(type: "text", nullable: true),
                    UserStrength = table.Column<string>(type: "text", nullable: true),
                    UserWeaknesses = table.Column<string>(type: "text", nullable: true),
                    MotivationalMessage = table.Column<string>(type: "text", nullable: true),
                    UniversitiestoConsider = table.Column<List<string>>(type: "text[]", nullable: true),
                    BursariesAvailable = table.Column<List<string>>(type: "text[]", nullable: true),
                    StudyTips = table.Column<string>(type: "text", nullable: true),
                    ImprovementtoRoadmap = table.Column<string>(type: "text", nullable: true),
                    SkillsToLearn = table.Column<List<string>>(type: "text[]", nullable: true),
                    FiveYearsOutLook = table.Column<string>(type: "text", nullable: true),
                    SalaryRange = table.Column<string>(type: "text", nullable: true),
                    RiskAssessment = table.Column<string>(type: "text", nullable: true),
                    TeacherRecommendation = table.Column<string>(type: "text", nullable: true),
                    ParentSummary = table.Column<string>(type: "text", nullable: true),
                    SubjectChangeSuggestion = table.Column<List<string>>(type: "text[]", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiResponse", x => x.AiResponseId);
                    table.ForeignKey(
                        name: "FK_AiResponse_ApsAnalysiss_ApsAnalysisId",
                        column: x => x.ApsAnalysisId,
                        principalTable: "ApsAnalysiss",
                        principalColumn: "ApsAnalysisId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CareerMaths",
                columns: table => new
                {
                    CareerMatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Field = table.Column<string>(type: "text", nullable: true),
                    MatchPercentage = table.Column<int>(type: "integer", nullable: false),
                    requiredSubjects = table.Column<string>(type: "text", nullable: true),
                    UniversityCourse = table.Column<string>(type: "text", nullable: true),
                    JobDescription = table.Column<string>(type: "text", nullable: true),
                    growthPotentials = table.Column<string>(type: "text", nullable: true),
                    SalaryRange = table.Column<string>(type: "text", nullable: true),
                    TimeToQualify = table.Column<string>(type: "text", nullable: true),
                    TopCompaniesHiring = table.Column<List<string>>(type: "text[]", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AiResponseId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerMaths", x => x.CareerMatchId);
                    table.ForeignKey(
                        name: "FK_CareerMaths_AiResponse_AiResponseId",
                        column: x => x.AiResponseId,
                        principalTable: "AiResponse",
                        principalColumn: "AiResponseId");
                });

            migrationBuilder.CreateTable(
                name: "DemandingCareerAssessments",
                columns: table => new
                {
                    DemandingCareerAssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerTitle = table.Column<string>(type: "text", nullable: true),
                    WhyitIsInDemand = table.Column<string>(type: "text", nullable: true),
                    GlobalDemandLevel = table.Column<string>(type: "text", nullable: true),
                    SalaryRange = table.Column<string>(type: "text", nullable: true),
                    CanStudentQualify = table.Column<bool>(type: "boolean", nullable: false),
                    QualificationVerdict = table.Column<string>(type: "text", nullable: true),
                    ReasonForVerdict = table.Column<string>(type: "text", nullable: true),
                    ChancesifTheyOpt = table.Column<int>(type: "integer", nullable: false),
                    WhatTheyNeedToSuccess = table.Column<string>(type: "text", nullable: true),
                    HonestyMessage = table.Column<string>(type: "text", nullable: true),
                    SubjectsTheyAreMissing = table.Column<List<string>>(type: "text[]", nullable: true),
                    AlternativeRoute = table.Column<string>(type: "text", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AiResponseId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandingCareerAssessments", x => x.DemandingCareerAssessmentId);
                    table.ForeignKey(
                        name: "FK_DemandingCareerAssessments_AiResponse_AiResponseId",
                        column: x => x.AiResponseId,
                        principalTable: "AiResponse",
                        principalColumn: "AiResponseId");
                });

            migrationBuilder.CreateTable(
                name: "DyingCareerWarning",
                columns: table => new
                {
                    DyingCareerWarningId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerTitle = table.Column<string>(type: "text", nullable: true),
                    WhyItIsDying = table.Column<string>(type: "text", nullable: true),
                    JobAvailabilityIn5Years = table.Column<int>(type: "integer", nullable: false),
                    ChanceOfGettingJobAfterStudying = table.Column<int>(type: "integer", nullable: false),
                    Honestwarning = table.Column<string>(type: "text", nullable: true),
                    MotivationalRedirect = table.Column<string>(type: "text", nullable: true),
                    BetterAlternative = table.Column<string>(type: "text", nullable: true),
                    IsRelevanttoStudent = table.Column<bool>(type: "boolean", nullable: false),
                    RelevanceReason = table.Column<string>(type: "text", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AiResponseId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DyingCareerWarning", x => x.DyingCareerWarningId);
                    table.ForeignKey(
                        name: "FK_DyingCareerWarning_AiResponse_AiResponseId",
                        column: x => x.AiResponseId,
                        principalTable: "AiResponse",
                        principalColumn: "AiResponseId");
                });

            migrationBuilder.CreateTable(
                name: "EmploymentOutlooks",
                columns: table => new
                {
                    EmploymentOutlookId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerTitle = table.Column<string>(type: "text", nullable: true),
                    ChanceOfEmploymentAfterGraduation = table.Column<int>(type: "integer", nullable: false),
                    AverageTimeToGetFirstJob = table.Column<string>(type: "text", nullable: true),
                    JobMarketCompetition = table.Column<string>(type: "text", nullable: true),
                    SouthAfricanMarketInsight = table.Column<string>(type: "text", nullable: true),
                    GlobalOpportunities = table.Column<string>(type: "text", nullable: true),
                    TopIndustriesHiring = table.Column<List<string>>(type: "text[]", nullable: true),
                    EntryLevelSalary = table.Column<string>(type: "text", nullable: true),
                    SeniorLevelSalary = table.Column<string>(type: "text", nullable: true),
                    OutlookSummry = table.Column<string>(type: "text", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AiResponseId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentOutlooks", x => x.EmploymentOutlookId);
                    table.ForeignKey(
                        name: "FK_EmploymentOutlooks_AiResponse_AiResponseId",
                        column: x => x.AiResponseId,
                        principalTable: "AiResponse",
                        principalColumn: "AiResponseId");
                });

            migrationBuilder.CreateTable(
                name: "SubjectResults",
                columns: table => new
                {
                    SubjectResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    Mark = table.Column<int>(type: "integer", nullable: false),
                    Grade = table.Column<string>(type: "text", nullable: true),
                    CareerRelevance = table.Column<string>(type: "text", nullable: true),
                    ImprovementTip = table.Column<string>(type: "text", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcademicRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    AiResponseId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectResults", x => x.SubjectResultId);
                    table.ForeignKey(
                        name: "FK_SubjectResults_AcademicRecords_AcademicRecordId",
                        column: x => x.AcademicRecordId,
                        principalTable: "AcademicRecords",
                        principalColumn: "AcadmicRecordId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubjectResults_AiResponse_AiResponseId",
                        column: x => x.AiResponseId,
                        principalTable: "AiResponse",
                        principalColumn: "AiResponseId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiResponse_ApsAnalysisId",
                table: "AiResponse",
                column: "ApsAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_ApsAnalysiss_ImprovementAdviceId",
                table: "ApsAnalysiss",
                column: "ImprovementAdviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerMaths_AiResponseId",
                table: "CareerMaths",
                column: "AiResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_ApplicationUserId",
                table: "CreditTransactions",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_UserId",
                table: "CreditTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandingCareerAssessments_AiResponseId",
                table: "DemandingCareerAssessments",
                column: "AiResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_DyingCareerWarning_AiResponseId",
                table: "DyingCareerWarning",
                column: "AiResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentOutlooks_AiResponseId",
                table: "EmploymentOutlooks",
                column: "AiResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedSubjects_ExtractedAcademicRecordExtractionAcademic~",
                table: "ExtractedSubjects",
                column: "ExtractedAcademicRecordExtractionAcademicRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ApplicationUserId",
                table: "PaymentTransactions",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PlanId",
                table: "PaymentTransactions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Reference",
                table: "PaymentTransactions",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plans_Code",
                table: "Plans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PsychometricAssessments_ApplicationUserId",
                table: "PsychometricAssessments",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PsychometricAssessments_PsychometricProfileId",
                table: "PsychometricAssessments",
                column: "PsychometricProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PsychometricProfiles_ApplicationUserId",
                table: "PsychometricProfiles",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectResults_AcademicRecordId",
                table: "SubjectResults",
                column: "AcademicRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectResults_AiResponseId",
                table: "SubjectResults",
                column: "AiResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageTransactions_ApplicationUserId",
                table: "UsageTransactions",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageTransactions_UserId_CreatedAtUtc",
                table: "UsageTransactions",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_ApplicationUserId",
                table: "UserSubscriptions",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PlanId",
                table: "UserSubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_Status",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CareerMaths");

            migrationBuilder.DropTable(
                name: "CareerProfiles");

            migrationBuilder.DropTable(
                name: "CreditTransactions");

            migrationBuilder.DropTable(
                name: "DemandingCareerAssessments");

            migrationBuilder.DropTable(
                name: "DyingCareerWarning");

            migrationBuilder.DropTable(
                name: "EmploymentOutlooks");

            migrationBuilder.DropTable(
                name: "ExtractedSubjects");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "PsychometricAssessments");

            migrationBuilder.DropTable(
                name: "SubjectResults");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "UniveristyQualifications");

            migrationBuilder.DropTable(
                name: "UsageTransactions");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "ExtractedAcademicRecords");

            migrationBuilder.DropTable(
                name: "PsychometricProfiles");

            migrationBuilder.DropTable(
                name: "AcademicRecords");

            migrationBuilder.DropTable(
                name: "AiResponse");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "ApsAnalysiss");

            migrationBuilder.DropTable(
                name: "ImprovementAdvices");
        }
    }
}
