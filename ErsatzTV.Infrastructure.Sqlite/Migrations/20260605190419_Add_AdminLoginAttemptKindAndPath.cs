using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_AdminLoginAttemptKindAndPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptKind",
                table: "AdminLoginAttempt",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RequestPath",
                table: "AdminLoginAttempt",
                type: "TEXT",
                nullable: true,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptKind",
                table: "AdminLoginAttempt");

            migrationBuilder.DropColumn(
                name: "RequestPath",
                table: "AdminLoginAttempt");
        }
    }
}
