using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pathly_Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProofilePictures = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImprovementAdvices",
                columns: table => new
                {
                    ImprovementAdviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShouldReWriteMatric = table.Column<bool>(type: "bit", nullable: false),
                    ShouldUpgradeSubjects = table.Column<bool>(type: "bit", nullable: false),
                    RecommendedSubjecrsToImprove = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlternativeOptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MotivationalGuidance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImprovementAdvices", x => x.ImprovementAdviceId);
                });

            migrationBuilder.CreateTable(
                name: "UniveristyQualifications",
                columns: table => new
                {
                    UnviversityQualificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinimumAPS = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendedCourse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gap = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniveristyQualifications", x => x.UnviversityQualificationId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
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
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
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
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "ApsAnalysiss",
                columns: table => new
                {
                    ApsAnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CalculatedAps = table.Column<int>(type: "int", nullable: false),
                    ApsExplanation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QualifiesForUniveisty = table.Column<bool>(type: "bit", nullable: false),
                    QualificationMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UniversitiesTheyQualifyFor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UniversitiestheyDoNotQualifyFor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImprovementAdviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ImprovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "AiResponse",
                columns: table => new
                {
                    AiResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserFullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Grade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApsAnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverallScore = table.Column<int>(type: "int", nullable: false),
                    AcademicPersonality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeedBack = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserStrength = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserWeaknesses = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MotivationalMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UniversitiestoConsider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BursariesAvailable = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudyTips = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImprovementtoRoadmap = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SkillsToLearn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FiveYearsOutLook = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SalaryRange = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RiskAssessment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TeacherRecommendation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubjectChangeSuggestion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    CareerMatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatchPercentage = table.Column<int>(type: "int", nullable: false),
                    requiredSubjects = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UniversityCourse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    growthPotentials = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SalaryRange = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeToQualify = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopCompaniesHiring = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AiResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                    DemandingCareerAssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CareerTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WhyitIsInDemand = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GlobalDemandLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SalaryRange = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CanStudentQualify = table.Column<bool>(type: "bit", nullable: false),
                    QualificationVerdict = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReasonForVerdict = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChancesifTheyOpt = table.Column<int>(type: "int", nullable: false),
                    WhatTheyNeedToSuccess = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HonestyMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubjectsTheyAreMissing = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlternativeRoute = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AiResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                    DyingCareerWarningId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CareerTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WhyItIsDying = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobAvailabilityIn5Years = table.Column<int>(type: "int", nullable: false),
                    ChanceOfGettingJobAfterStudying = table.Column<int>(type: "int", nullable: false),
                    Honestwarning = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MotivationalRedirect = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BetterAlternative = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRelevanttoStudent = table.Column<bool>(type: "bit", nullable: false),
                    RelevanceReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AiResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                    EmploymentOutlookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CareerTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChanceOfEmploymentAfterGraduation = table.Column<int>(type: "int", nullable: false),
                    AverageTimeToGetFirstJob = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobMarketCompetition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SouthAfricanMarketInsight = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GlobalOpportunities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopIndustriesHiring = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntryLevelSalary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SeniorLevelSalary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutlookSummry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AiResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                    SubjectResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subect = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mark = table.Column<int>(type: "int", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CareerRelevance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImprovementTip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AiResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectResults", x => x.SubjectResultId);
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
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

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
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CareerMaths_AiResponseId",
                table: "CareerMaths",
                column: "AiResponseId");

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
                name: "IX_SubjectResults_AiResponseId",
                table: "SubjectResults",
                column: "AiResponseId");
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
                name: "DemandingCareerAssessments");

            migrationBuilder.DropTable(
                name: "DyingCareerWarning");

            migrationBuilder.DropTable(
                name: "EmploymentOutlooks");

            migrationBuilder.DropTable(
                name: "SubjectResults");

            migrationBuilder.DropTable(
                name: "UniveristyQualifications");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "AiResponse");

            migrationBuilder.DropTable(
                name: "ApsAnalysiss");

            migrationBuilder.DropTable(
                name: "ImprovementAdvices");
        }
    }
}
