using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pathly_Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOwnedAnalysisHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AspNetUsers.Password is stale drift from an old model — the current Identity-based
            // auth never reads it, so drop it rather than let EF mis-guess a rename.
            migrationBuilder.DropColumn(
                name: "Password",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "TermsVersion",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MarketingConsent",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAtUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "AiResponse",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverTermLabel",
                table: "AiResponse",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExtractionAcademicRecordId",
                table: "AiResponse",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiResponse_ApplicationUserId_AddedAt",
                table: "AiResponse",
                columns: new[] { "ApplicationUserId", "AddedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_AiResponse_AspNetUsers_ApplicationUserId",
                table: "AiResponse",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiResponse_AspNetUsers_ApplicationUserId",
                table: "AiResponse");

            migrationBuilder.DropIndex(
                name: "IX_AiResponse_ApplicationUserId_AddedAt",
                table: "AiResponse");

            migrationBuilder.DropColumn(
                name: "MarketingConsent",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TermsVersion",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "AiResponse");

            migrationBuilder.DropColumn(
                name: "DriverTermLabel",
                table: "AiResponse");

            migrationBuilder.DropColumn(
                name: "ExtractionAcademicRecordId",
                table: "AiResponse");

            migrationBuilder.RenameColumn(
                name: "TermsVersion",
                table: "AspNetUsers",
                newName: "Password");
        }
    }
}
