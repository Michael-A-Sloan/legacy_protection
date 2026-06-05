using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
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
                type: "longtext",
                nullable: true,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IpAddressV6",
                table: "AdminLoginAttempt",
                type: "longtext",
                nullable: true,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
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
