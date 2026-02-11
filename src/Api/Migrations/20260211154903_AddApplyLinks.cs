using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerAgent.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddApplyLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplyLinks",
                table: "JobListings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplyLinks",
                table: "JobListings");
        }
    }
}
