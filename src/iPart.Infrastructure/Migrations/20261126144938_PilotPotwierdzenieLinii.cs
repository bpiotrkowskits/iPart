using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iPart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PilotPotwierdzenieLinii : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConfirmationBasis",
                schema: "documentation",
                table: "BomLines",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmationNote",
                schema: "documentation",
                table: "BomLines",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConfirmedAtUtc",
                schema: "documentation",
                table: "BomLines",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmedBy",
                schema: "documentation",
                table: "BomLines",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmedByUserId",
                schema: "documentation",
                table: "BomLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_BomLines_ConfirmationBasis",
                schema: "documentation",
                table: "BomLines",
                sql: "[Confirmed] = 0 OR [ConfirmationBasis] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BomLines_ConfirmationBasis",
                schema: "documentation",
                table: "BomLines");

            migrationBuilder.DropColumn(
                name: "ConfirmationBasis",
                schema: "documentation",
                table: "BomLines");

            migrationBuilder.DropColumn(
                name: "ConfirmationNote",
                schema: "documentation",
                table: "BomLines");

            migrationBuilder.DropColumn(
                name: "ConfirmedAtUtc",
                schema: "documentation",
                table: "BomLines");

            migrationBuilder.DropColumn(
                name: "ConfirmedBy",
                schema: "documentation",
                table: "BomLines");

            migrationBuilder.DropColumn(
                name: "ConfirmedByUserId",
                schema: "documentation",
                table: "BomLines");
        }
    }
}
