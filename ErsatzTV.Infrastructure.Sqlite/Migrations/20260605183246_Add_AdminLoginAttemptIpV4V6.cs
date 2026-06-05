using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_AdminLoginAttemptIpV4V6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IpAddressV4",
                table: "AdminLoginAttempt",
                type: "TEXT",
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IpAddressV6",
                table: "AdminLoginAttempt",
                type: "TEXT",
                nullable: true,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpAddressV4",
                table: "AdminLoginAttempt");

            migrationBuilder.DropColumn(
                name: "IpAddressV6",
                table: "AdminLoginAttempt");
        }
    }
}
