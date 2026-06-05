using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_AdminLoginSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminIpRule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: false),
                    RuleType = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true, defaultValue: ""),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminIpRule", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminLoginAttempt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true, defaultValue: ""),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    DenyReason = table.Column<string>(type: "TEXT", nullable: true, defaultValue: ""),
                    UserAgent = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminLoginAttempt", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminIpRule_IpAddress_RuleType",
                table: "AdminIpRule",
                columns: new[] { "IpAddress", "RuleType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminLoginAttempt_IpAddress",
                table: "AdminLoginAttempt",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AdminLoginAttempt_Timestamp",
                table: "AdminLoginAttempt",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminIpRule");

            migrationBuilder.DropTable(
                name: "AdminLoginAttempt");
        }
    }
}
