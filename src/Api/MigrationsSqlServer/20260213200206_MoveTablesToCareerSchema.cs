using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerAgent.Api.MigrationsSqlServer
{
    /// <inheritdoc />
    public partial class MoveTablesToCareerSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "career");

            migrationBuilder.RenameTable(
                name: "TailoredDocuments",
                newName: "TailoredDocuments",
                newSchema: "career");

            migrationBuilder.RenameTable(
                name: "SearchProfiles",
                newName: "SearchProfiles",
                newSchema: "career");

            migrationBuilder.RenameTable(
                name: "MasterResumes",
                newName: "MasterResumes",
                newSchema: "career");

            migrationBuilder.RenameTable(
                name: "JobListings",
                newName: "JobListings",
                newSchema: "career");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "TailoredDocuments",
                schema: "career",
                newName: "TailoredDocuments");

            migrationBuilder.RenameTable(
                name: "SearchProfiles",
                schema: "career",
                newName: "SearchProfiles");

            migrationBuilder.RenameTable(
                name: "MasterResumes",
                schema: "career",
                newName: "MasterResumes");

            migrationBuilder.RenameTable(
                name: "JobListings",
                schema: "career",
                newName: "JobListings");
        }
    }
}
