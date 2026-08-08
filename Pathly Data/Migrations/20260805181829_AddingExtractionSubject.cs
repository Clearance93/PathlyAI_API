using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pathly_Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingExtractionSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExtractedSubject_ExtractedAcademicRecords_ExtractedAcademicRecordExtractionAcademicRecordId",
                table: "ExtractedSubject");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExtractedSubject",
                table: "ExtractedSubject");

            migrationBuilder.RenameTable(
                name: "ExtractedSubject",
                newName: "ExtractedSubjects");

            migrationBuilder.RenameIndex(
                name: "IX_ExtractedSubject_ExtractedAcademicRecordExtractionAcademicRecordId",
                table: "ExtractedSubjects",
                newName: "IX_ExtractedSubjects_ExtractedAcademicRecordExtractionAcademicRecordId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExtractedSubjects",
                table: "ExtractedSubjects",
                column: "ExtractionSubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExtractedSubjects_ExtractedAcademicRecords_ExtractedAcademicRecordExtractionAcademicRecordId",
                table: "ExtractedSubjects",
                column: "ExtractedAcademicRecordExtractionAcademicRecordId",
                principalTable: "ExtractedAcademicRecords",
                principalColumn: "ExtractionAcademicRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExtractedSubjects_ExtractedAcademicRecords_ExtractedAcademicRecordExtractionAcademicRecordId",
                table: "ExtractedSubjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExtractedSubjects",
                table: "ExtractedSubjects");

            migrationBuilder.RenameTable(
                name: "ExtractedSubjects",
                newName: "ExtractedSubject");

            migrationBuilder.RenameIndex(
                name: "IX_ExtractedSubjects_ExtractedAcademicRecordExtractionAcademicRecordId",
                table: "ExtractedSubject",
                newName: "IX_ExtractedSubject_ExtractedAcademicRecordExtractionAcademicRecordId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExtractedSubject",
                table: "ExtractedSubject",
                column: "ExtractionSubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExtractedSubject_ExtractedAcademicRecords_ExtractedAcademicRecordExtractionAcademicRecordId",
                table: "ExtractedSubject",
                column: "ExtractedAcademicRecordExtractionAcademicRecordId",
                principalTable: "ExtractedAcademicRecords",
                principalColumn: "ExtractionAcademicRecordId");
        }
    }
}
