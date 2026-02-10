using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerAgent.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobListings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Company = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Salary = table.Column<string>(type: "TEXT", nullable: true),
                    RelevanceScore = table.Column<double>(type: "REAL", nullable: false),
                    MatchedSkills = table.Column<string>(type: "TEXT", nullable: false),
                    MissingSkills = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobListings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterResumes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    RawMarkdown = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterResumes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Query = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    RadiusMiles = table.Column<int>(type: "INTEGER", nullable: false),
                    RemoteOnly = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiredSkills = table.Column<string>(type: "TEXT", nullable: false),
                    PreferredSkills = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TailoredDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobListingId = table.Column<int>(type: "INTEGER", nullable: false),
                    MasterResumeId = table.Column<int>(type: "INTEGER", nullable: false),
                    TailoredResumeMarkdown = table.Column<string>(type: "TEXT", nullable: false),
                    CoverLetterMarkdown = table.Column<string>(type: "TEXT", nullable: false),
                    PdfPath = table.Column<string>(type: "TEXT", nullable: true),
                    ClaudePrompt = table.Column<string>(type: "TEXT", nullable: false),
                    ClaudeResponse = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TailoredDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TailoredDocuments_JobListings_JobListingId",
                        column: x => x.JobListingId,
                        principalTable: "JobListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TailoredDocuments_MasterResumes_MasterResumeId",
                        column: x => x.MasterResumeId,
                        principalTable: "MasterResumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobListings_ExternalId_Source",
                table: "JobListings",
                columns: new[] { "ExternalId", "Source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobListings_RelevanceScore",
                table: "JobListings",
                column: "RelevanceScore");

            migrationBuilder.CreateIndex(
                name: "IX_JobListings_Status",
                table: "JobListings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TailoredDocuments_JobListingId",
                table: "TailoredDocuments",
                column: "JobListingId");

            migrationBuilder.CreateIndex(
                name: "IX_TailoredDocuments_MasterResumeId",
                table: "TailoredDocuments",
                column: "MasterResumeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchProfiles");

            migrationBuilder.DropTable(
                name: "TailoredDocuments");

            migrationBuilder.DropTable(
                name: "JobListings");

            migrationBuilder.DropTable(
                name: "MasterResumes");
        }
    }
}
