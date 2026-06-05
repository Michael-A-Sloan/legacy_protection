using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IpAddress = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RuleType = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "longtext", nullable: true, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminIpRule", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdminLoginAttempt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IpAddress = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Username = table.Column<string>(type: "longtext", nullable: true, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Success = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DenyReason = table.Column<string>(type: "longtext", nullable: true, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "longtext", nullable: true, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminLoginAttempt", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
            migrationBuilder.DropTable(name: "AdminIpRule");
            migrationBuilder.DropTable(name: "AdminLoginAttempt");
        }
    }
}
