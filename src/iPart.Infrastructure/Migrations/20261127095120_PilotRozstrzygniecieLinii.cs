using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iPart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PilotRozstrzygniecieLinii : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "documentation",
                table: "PnpRows",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TsDescription",
                schema: "documentation",
                table: "BomLines",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TsFootprint",
                schema: "documentation",
                table: "BomLines",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                schema: "documentation",
                table: "PnpRows");

            migrationBuilder.DropColumn(
                name: "TsDescription",
                schema: "documentation",
                table: "BomLines");

            migrationBuilder.DropColumn(
                name: "TsFootprint",
                schema: "documentation",
                table: "BomLines");
        }
    }
}
