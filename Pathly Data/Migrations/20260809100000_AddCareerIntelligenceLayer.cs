using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pathly_Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCareerIntelligenceLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnalysisVersion",
                table: "AiResponse",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptVersion",
                table: "AiResponse",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PsychometricHash",
                table: "AiResponse",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPremium",
                table: "AiResponse",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AiResponse_PsychometricHash",
                table: "AiResponse",
                column: "PsychometricHash");

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CanonicalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.SubjectId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_NormalizedName",
                table: "Subjects",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateTable(
                name: "PsychometricProfiles",
                columns: table => new
                {
                    PsychometricProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Realistic = table.Column<int>(type: "int", nullable: false),
                    Investigative = table.Column<int>(type: "int", nullable: false),
                    Artistic = table.Column<int>(type: "int", nullable: false),
                    Social = table.Column<int>(type: "int", nullable: false),
                    Enterprising = table.Column<int>(type: "int", nullable: false),
                    Conventional = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsychometricProfiles", x => x.PsychometricProfileId);
                });

            migrationBuilder.CreateTable(
                name: "CareerProfiles",
                columns: table => new
                {
                    CareerProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CareerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequiredSubjects = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinimumAps = table.Column<int>(type: "int", nullable: false),
                    RealisticWeight = table.Column<int>(type: "int", nullable: false),
                    InvestigativeWeight = table.Column<int>(type: "int", nullable: false),
                    ArtisticWeight = table.Column<int>(type: "int", nullable: false),
                    SocialWeight = table.Column<int>(type: "int", nullable: false),
                    EnterprisingWeight = table.Column<int>(type: "int", nullable: false),
                    ConventionalWeight = table.Column<int>(type: "int", nullable: false),
                    DemandScore = table.Column<int>(type: "int", nullable: false),
                    GrowthScore = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerProfiles", x => x.CareerProfileId);
                });

            // Seed a starter career knowledge base (Part 9) spanning established, technology, and
            // emerging/green categories. Weights/scores are Pathly's own editorial estimates,
            // not sourced from an external labour-market dataset — treat as a reasonable
            // starting point to refine over time, not as ground truth.
            migrationBuilder.InsertData(
                table: "CareerProfiles",
                columns: new[]
                {
                    "CareerProfileId", "CareerName", "Category", "RequiredSubjects", "MinimumAps",
                    "RealisticWeight", "InvestigativeWeight", "ArtisticWeight", "SocialWeight",
                    "EnterprisingWeight", "ConventionalWeight", "DemandScore", "GrowthScore", "Description"
                },
                values: new object[,]
                {
                    {
                        Guid.Parse("9a1f1e10-0001-4a2b-8c3d-000000000001"), "Mechanical Engineering", "Established",
                        "Mathematics,Physical Sciences", 32, 80, 85, 20, 20, 30, 40, 68, 60,
                        "Designs and builds mechanical systems and machinery."
                    },
                    {
                        Guid.Parse("9a1f1e10-0001-4a2b-8c3d-000000000002"), "Software Engineering", "Technology",
                        "Mathematics,Information Technology", 30, 40, 90, 30, 20, 40, 50, 88, 90,
                        "Designs, builds, and maintains software systems and applications."
                    },
                    {
                        Guid.Parse("9a1f1e10-0001-4a2b-8c3d-000000000003"), "Data Science", "Technology",
                        "Mathematics,Information Technology", 32, 30, 95, 20, 20, 30, 60, 85, 92,
                        "Extracts insight from data using statistics, programming, and modelling."
                    },
                    {
                        Guid.Parse("9a1f1e10-0001-4a2b-8c3d-000000000004"), "Cybersecurity Analyst", "Technology",
                        "Mathematics,Information Technology", 28, 50, 85, 10, 20, 20, 60, 84, 88,
                        "Protects systems and data from unauthorized access and attacks."
                    },
                    {
                        Guid.Parse("9a1f1e10-0001-4a2b-8c3d-000000000005"), "Robotics Engineering", "Emerging",
                        "Mathematics,Physical Sciences,Information Technology", 34, 85, 85, 30, 15, 25, 40, 65, 85,
                        "Designs and programs robotic and automated systems."
                    },
                    {
                        Guid.Parse("9a1f1e10-0001-4a2b-8c3d-000000000006"), "Renewable Energy Engineering", "Green Technology",
                        "Mathematics,Physical Sciences", 30, 75, 80, 15, 25, 30, 35, 70, 90,
                        "Designs and implements solar, wind, and other renewable energy systems."
                    },
                    {
                        Guid.Parse("9a1f1e10-0001-4a2b-8c3d-000000000007"), "Biotechnology Research", "Emerging",
                        "Life Sciences,Physical Sciences,Mathematics", 32, 40, 95, 15, 30, 15, 40, 60, 82,
                        "Applies biology and technology to develop medical, agricultural, and industrial innovations."
                    },
                    {
                        Guid.Parse("9a1f1e10-0001-4a2b-8c3d-000000000008"), "Chartered Accountancy", "Established",
                        "Mathematics,Accounting", 30, 10, 40, 10, 30, 45, 90, 70, 55,
                        "Manages financial reporting, auditing, and tax compliance for organizations."
                    },
                    {
                        Guid.Parse("9a1f1e10-0001-4a2b-8c3d-000000000009"), "Medicine (MBChB)", "Established",
                        "Life Sciences,Physical Sciences,Mathematics", 40, 30, 80, 10, 90, 20, 35, 90, 65,
                        "Diagnoses and treats illness and injury as a medical doctor."
                    },
                    {
                        Guid.Parse("9a1f1e10-0001-4a2b-8c3d-00000000000a"), "Digital Marketing & Entrepreneurship", "Emerging",
                        "Business Studies,Information Technology", 24, 15, 30, 55, 40, 90, 30, 75, 78,
                        "Builds and grows businesses and brands through digital channels and new ventures."
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CareerProfiles");

            migrationBuilder.DropTable(name: "PsychometricProfiles");

            migrationBuilder.DropTable(name: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_AiResponse_PsychometricHash",
                table: "AiResponse");

            migrationBuilder.DropColumn(name: "IsPremium", table: "AiResponse");

            migrationBuilder.DropColumn(name: "PsychometricHash", table: "AiResponse");

            migrationBuilder.DropColumn(name: "PromptVersion", table: "AiResponse");

            migrationBuilder.DropColumn(name: "AnalysisVersion", table: "AiResponse");
        }
    }
}
