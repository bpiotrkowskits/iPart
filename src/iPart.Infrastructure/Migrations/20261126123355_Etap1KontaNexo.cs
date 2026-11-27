using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iPart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Etap1KontaNexo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_AccountName",
                schema: "shared",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Active",
                schema: "shared",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "shared",
                table: "Users",
                newName: "LastLoginAtUtc");

            migrationBuilder.RenameColumn(
                name: "AccountName",
                schema: "shared",
                table: "Users",
                newName: "Login");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                schema: "shared",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "shared",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstLoginAtUtc",
                schema: "shared",
                table: "Users",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login",
                schema: "shared",
                table: "Users",
                column: "Login");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Login",
                schema: "shared",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "shared",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "shared",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FirstLoginAtUtc",
                schema: "shared",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "Login",
                schema: "shared",
                table: "Users",
                newName: "AccountName");

            migrationBuilder.RenameColumn(
                name: "LastLoginAtUtc",
                schema: "shared",
                table: "Users",
                newName: "CreatedAtUtc");

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                schema: "shared",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Users_AccountName",
                schema: "shared",
                table: "Users",
                column: "AccountName",
                unique: true);
        }
    }
}
