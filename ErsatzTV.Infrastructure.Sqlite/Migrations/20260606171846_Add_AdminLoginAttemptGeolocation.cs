using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_AdminLoginAttemptGeolocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "AdminLoginAttempt",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LocationAccuracyMeters",
                table: "AdminLoginAttempt",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "AdminLoginAttempt",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "AdminLoginAttempt");

            migrationBuilder.DropColumn(
                name: "LocationAccuracyMeters",
                table: "AdminLoginAttempt");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "AdminLoginAttempt");
        }
    }
}
