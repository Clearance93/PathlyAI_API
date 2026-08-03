using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pathly_Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Subect",
                table: "SubjectResults",
                newName: "Subject");

            migrationBuilder.RenameColumn(
                name: "QualifiesForUniveisty",
                table: "ApsAnalysiss",
                newName: "QualifiesForUniversity");

            migrationBuilder.AddColumn<Guid>(
                name: "AcademicRecordId",
                table: "SubjectResults",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<double>(
                name: "OverallScore",
                table: "AiResponse",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "ResponseJson",
                table: "AiResponse",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AcademicRecords",
                columns: table => new
                {
                    AcadmicRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Grade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClalculatedAPS = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicRecords", x => x.AcadmicRecordId);
                });

            migrationBuilder.CreateTable(
                name: "ExtractedAcademicRecords",
                columns: table => new
                {
                    ExtractionAcademicRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudyLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawExtractedText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtractedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractedAcademicRecords", x => x.ExtractionAcademicRecordId);
                });

            migrationBuilder.CreateTable(
                name: "ExtractedSubjects",
                columns: table => new
                {
                    ExtractionSubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawMark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumericMark = table.Column<int>(type: "int", nullable: true),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MarkType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtractedAcademicRecordExtractionAcademicRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractedSubjects", x => x.ExtractionSubjectId);
                    table.ForeignKey(
                        name: "FK_ExtractedSubjects_ExtractedAcademicRecords_ExtractedAcademicRecordExtractionAcademicRecordId",
                        column: x => x.ExtractedAcademicRecordExtractionAcademicRecordId,
                        principalTable: "ExtractedAcademicRecords",
                        principalColumn: "ExtractionAcademicRecordId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubjectResults_AcademicRecordId",
                table: "SubjectResults",
                column: "AcademicRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedSubjects_ExtractedAcademicRecordExtractionAcademicRecordId",
                table: "ExtractedSubjects",
                column: "ExtractedAcademicRecordExtractionAcademicRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubjectResults_AcademicRecords_AcademicRecordId",
                table: "SubjectResults",
                column: "AcademicRecordId",
                principalTable: "AcademicRecords",
                principalColumn: "AcadmicRecordId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubjectResults_AcademicRecords_AcademicRecordId",
                table: "SubjectResults");

            migrationBuilder.DropTable(
                name: "AcademicRecords");

            migrationBuilder.DropTable(
                name: "ExtractedSubjects");

            migrationBuilder.DropTable(
                name: "ExtractedAcademicRecords");

            migrationBuilder.DropIndex(
                name: "IX_SubjectResults_AcademicRecordId",
                table: "SubjectResults");

            migrationBuilder.DropColumn(
                name: "AcademicRecordId",
                table: "SubjectResults");

            migrationBuilder.DropColumn(
                name: "ResponseJson",
                table: "AiResponse");

            migrationBuilder.RenameColumn(
                name: "Subject",
                table: "SubjectResults",
                newName: "Subect");

            migrationBuilder.RenameColumn(
                name: "QualifiesForUniversity",
                table: "ApsAnalysiss",
                newName: "QualifiesForUniveisty");

            migrationBuilder.AlterColumn<int>(
                name: "OverallScore",
                table: "AiResponse",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");
        }
    }
}
