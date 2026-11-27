using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iPart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PilotWspolrzedneMm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "XMm",
                schema: "documentation",
                table: "PnpRows",
                type: "decimal(12,4)",
                precision: 12,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "YMm",
                schema: "documentation",
                table: "PnpRows",
                type: "decimal(12,4)",
                precision: 12,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "XMm",
                schema: "documentation",
                table: "PnpRows");

            migrationBuilder.DropColumn(
                name: "YMm",
                schema: "documentation",
                table: "PnpRows");
        }
    }
}
