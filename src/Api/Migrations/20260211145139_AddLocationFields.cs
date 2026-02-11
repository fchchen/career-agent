using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerAgent.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRemote",
                table: "JobListings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "JobListings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "JobListings",
                type: "REAL",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobListings_IsRemote",
                table: "JobListings",
                column: "IsRemote");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobListings_IsRemote",
                table: "JobListings");

            migrationBuilder.DropColumn(
                name: "IsRemote",
                table: "JobListings");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "JobListings");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "JobListings");
        }
    }
}
