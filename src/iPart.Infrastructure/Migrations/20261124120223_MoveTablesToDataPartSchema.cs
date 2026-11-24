using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iPart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveTablesToDataPartSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dataPart");

            migrationBuilder.RenameTable(
                name: "Subcategories",
                newName: "Subcategories",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "ResistorParameters",
                newName: "ResistorParameters",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "ProductStatuses",
                newName: "ProductStatuses",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "Packages",
                newName: "Packages",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "MountingMethods",
                newName: "MountingMethods",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "Manufacturers",
                newName: "Manufacturers",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "LedParameters",
                newName: "LedParameters",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "InductorParameters",
                newName: "InductorParameters",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "Customers",
                newName: "Customers",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "CrystalParameters",
                newName: "CrystalParameters",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "ConnectorParameters",
                newName: "ConnectorParameters",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "Components",
                newName: "Components",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "ComponentQualifications",
                newName: "ComponentQualifications",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "ComponentPads",
                newName: "ComponentPads",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "ComponentLinks",
                newName: "ComponentLinks",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "ComponentHistory",
                newName: "ComponentHistory",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "Categories",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "CasePatterns",
                newName: "CasePatterns",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "CapacitorParameters",
                newName: "CapacitorParameters",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "BomRevisions",
                newName: "BomRevisions",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "BomLines",
                newName: "BomLines",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "BomLineExtraValues",
                newName: "BomLineExtraValues",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "BomLineDesignators",
                newName: "BomLineDesignators",
                newSchema: "dataPart");

            migrationBuilder.RenameTable(
                name: "AecQualifications",
                newName: "AecQualifications",
                newSchema: "dataPart");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Subcategories",
                schema: "dataPart",
                newName: "Subcategories");

            migrationBuilder.RenameTable(
                name: "ResistorParameters",
                schema: "dataPart",
                newName: "ResistorParameters");

            migrationBuilder.RenameTable(
                name: "ProductStatuses",
                schema: "dataPart",
                newName: "ProductStatuses");

            migrationBuilder.RenameTable(
                name: "Packages",
                schema: "dataPart",
                newName: "Packages");

            migrationBuilder.RenameTable(
                name: "MountingMethods",
                schema: "dataPart",
                newName: "MountingMethods");

            migrationBuilder.RenameTable(
                name: "Manufacturers",
                schema: "dataPart",
                newName: "Manufacturers");

            migrationBuilder.RenameTable(
                name: "LedParameters",
                schema: "dataPart",
                newName: "LedParameters");

            migrationBuilder.RenameTable(
                name: "InductorParameters",
                schema: "dataPart",
                newName: "InductorParameters");

            migrationBuilder.RenameTable(
                name: "Customers",
                schema: "dataPart",
                newName: "Customers");

            migrationBuilder.RenameTable(
                name: "CrystalParameters",
                schema: "dataPart",
                newName: "CrystalParameters");

            migrationBuilder.RenameTable(
                name: "ConnectorParameters",
                schema: "dataPart",
                newName: "ConnectorParameters");

            migrationBuilder.RenameTable(
                name: "Components",
                schema: "dataPart",
                newName: "Components");

            migrationBuilder.RenameTable(
                name: "ComponentQualifications",
                schema: "dataPart",
                newName: "ComponentQualifications");

            migrationBuilder.RenameTable(
                name: "ComponentPads",
                schema: "dataPart",
                newName: "ComponentPads");

            migrationBuilder.RenameTable(
                name: "ComponentLinks",
                schema: "dataPart",
                newName: "ComponentLinks");

            migrationBuilder.RenameTable(
                name: "ComponentHistory",
                schema: "dataPart",
                newName: "ComponentHistory");

            migrationBuilder.RenameTable(
                name: "Categories",
                schema: "dataPart",
                newName: "Categories");

            migrationBuilder.RenameTable(
                name: "CasePatterns",
                schema: "dataPart",
                newName: "CasePatterns");

            migrationBuilder.RenameTable(
                name: "CapacitorParameters",
                schema: "dataPart",
                newName: "CapacitorParameters");

            migrationBuilder.RenameTable(
                name: "BomRevisions",
                schema: "dataPart",
                newName: "BomRevisions");

            migrationBuilder.RenameTable(
                name: "BomLines",
                schema: "dataPart",
                newName: "BomLines");

            migrationBuilder.RenameTable(
                name: "BomLineExtraValues",
                schema: "dataPart",
                newName: "BomLineExtraValues");

            migrationBuilder.RenameTable(
                name: "BomLineDesignators",
                schema: "dataPart",
                newName: "BomLineDesignators");

            migrationBuilder.RenameTable(
                name: "AecQualifications",
                schema: "dataPart",
                newName: "AecQualifications");
        }
    }
}
