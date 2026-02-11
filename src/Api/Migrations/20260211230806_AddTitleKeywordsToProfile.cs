using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerAgent.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTitleKeywordsToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NegativeTitleKeywords",
                table: "SearchProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TitleKeywords",
                table: "SearchProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // Clear existing profiles so the updated seed (with new fields) runs on next startup
            migrationBuilder.Sql("DELETE FROM SearchProfiles;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NegativeTitleKeywords",
                table: "SearchProfiles");

            migrationBuilder.DropColumn(
                name: "TitleKeywords",
                table: "SearchProfiles");
        }
    }
}
