using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pathly_Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPsychometricAssessmentReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReportJson",
                table: "PsychometricAssessments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportJson",
                table: "PsychometricAssessments");
        }
    }
}
