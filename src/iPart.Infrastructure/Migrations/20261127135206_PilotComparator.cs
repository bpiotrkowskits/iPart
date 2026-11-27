using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iPart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PilotComparator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompareFields",
                schema: "documentation",
                table: "BomRevisions",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SplitFromLineNumber",
                schema: "documentation",
                table: "BomLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TsValue",
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
                name: "CompareFields",
                schema: "documentation",
                table: "BomRevisions");

            migrationBuilder.DropColumn(
                name: "SplitFromLineNumber",
                schema: "documentation",
                table: "BomLines");

            migrationBuilder.DropColumn(
                name: "TsValue",
                schema: "documentation",
                table: "BomLines");
        }
    }
}
