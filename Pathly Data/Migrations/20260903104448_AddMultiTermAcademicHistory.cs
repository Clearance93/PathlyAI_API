using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pathly_Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTermAcademicHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AcademicPeriodId",
                table: "ExtractedSubjects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanonicalSubjectName",
                table: "ExtractedSubjects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinal",
                table: "ExtractedSubjects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TermLabel",
                table: "ExtractedSubjects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TermOrdinal",
                table: "ExtractedSubjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcademicYear",
                table: "ExtractedAcademicRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdmissionNo",
                table: "ExtractedAcademicRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "ExtractedAcademicRecords",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DriverIsFinal",
                table: "ExtractedAcademicRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DriverTermLabel",
                table: "ExtractedAcademicRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DriverTermOrdinal",
                table: "ExtractedAcademicRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LearnerNo",
                table: "ExtractedAcademicRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AcademicPeriods",
                columns: table => new
                {
                    AcademicPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsFinal = table.Column<bool>(type: "bit", nullable: false),
                    ExtractedAcademicRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicPeriods", x => x.AcademicPeriodId);
                    table.ForeignKey(
                        name: "FK_AcademicPeriods_ExtractedAcademicRecords_ExtractedAcademicRecordId",
                        column: x => x.ExtractedAcademicRecordId,
                        principalTable: "ExtractedAcademicRecords",
                        principalColumn: "ExtractionAcademicRecordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedSubjects_AcademicPeriodId",
                table: "ExtractedSubjects",
                column: "AcademicPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedAcademicRecords_ApplicationUserId",
                table: "ExtractedAcademicRecords",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPeriods_ExtractedAcademicRecordId_Ordinal",
                table: "AcademicPeriods",
                columns: new[] { "ExtractedAcademicRecordId", "Ordinal" });

            migrationBuilder.AddForeignKey(
                name: "FK_ExtractedAcademicRecords_AspNetUsers_ApplicationUserId",
                table: "ExtractedAcademicRecords",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ExtractedSubjects_AcademicPeriods_AcademicPeriodId",
                table: "ExtractedSubjects",
                column: "AcademicPeriodId",
                principalTable: "AcademicPeriods",
                principalColumn: "AcademicPeriodId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExtractedAcademicRecords_AspNetUsers_ApplicationUserId",
                table: "ExtractedAcademicRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ExtractedSubjects_AcademicPeriods_AcademicPeriodId",
                table: "ExtractedSubjects");

            migrationBuilder.DropTable(
                name: "AcademicPeriods");

            migrationBuilder.DropIndex(
                name: "IX_ExtractedSubjects_AcademicPeriodId",
                table: "ExtractedSubjects");

            migrationBuilder.DropIndex(
                name: "IX_ExtractedAcademicRecords_ApplicationUserId",
                table: "ExtractedAcademicRecords");

            migrationBuilder.DropColumn(
                name: "AcademicPeriodId",
                table: "ExtractedSubjects");

            migrationBuilder.DropColumn(
                name: "CanonicalSubjectName",
                table: "ExtractedSubjects");

            migrationBuilder.DropColumn(
                name: "IsFinal",
                table: "ExtractedSubjects");

            migrationBuilder.DropColumn(
                name: "TermLabel",
                table: "ExtractedSubjects");

            migrationBuilder.DropColumn(
                name: "TermOrdinal",
                table: "ExtractedSubjects");

            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "ExtractedAcademicRecords");

            migrationBuilder.DropColumn(
                name: "AdmissionNo",
                table: "ExtractedAcademicRecords");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "ExtractedAcademicRecords");

            migrationBuilder.DropColumn(
                name: "DriverIsFinal",
                table: "ExtractedAcademicRecords");

            migrationBuilder.DropColumn(
                name: "DriverTermLabel",
                table: "ExtractedAcademicRecords");

            migrationBuilder.DropColumn(
                name: "DriverTermOrdinal",
                table: "ExtractedAcademicRecords");

            migrationBuilder.DropColumn(
                name: "LearnerNo",
                table: "ExtractedAcademicRecords");
        }
    }
}
