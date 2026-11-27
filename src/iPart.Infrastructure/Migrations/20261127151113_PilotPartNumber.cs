using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iPart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PilotPartNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PartNumber",
                schema: "documentation",
                table: "PnpRows",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TsPartNumber",
                schema: "documentation",
                table: "BomLines",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartNumber",
                schema: "documentation",
                table: "PnpRows");

            migrationBuilder.DropColumn(
                name: "TsPartNumber",
                schema: "documentation",
                table: "BomLines");
        }
    }
}
