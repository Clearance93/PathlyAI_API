using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pathly_Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectSetHashCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubjectSetHash",
                table: "AiResponse",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiResponse_SubjectSetHash",
                table: "AiResponse",
                column: "SubjectSetHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiResponse_SubjectSetHash",
                table: "AiResponse");

            migrationBuilder.DropColumn(
                name: "SubjectSetHash",
                table: "AiResponse");
        }
    }
}
