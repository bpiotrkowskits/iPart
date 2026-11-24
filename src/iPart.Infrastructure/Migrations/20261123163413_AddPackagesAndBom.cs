using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iPart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPackagesAndBom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerRevision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Packages_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BomRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    SourceFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    SourceSheet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImportedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ImportedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomRevisions", x => x.Id);
                    table.CheckConstraint("CK_BomRevisions_Number", "[Number] >= 1");
                    table.ForeignKey(
                        name: "FK_BomRevisions_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BomLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BomRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    Designators = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    PartTs = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Footprint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PartNumber = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Confirmed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomLines", x => x.Id);
                    table.CheckConstraint("CK_BomLines_LineNumber", "[LineNumber] >= 1");
                    table.CheckConstraint("CK_BomLines_Quantity", "[Quantity] >= 0");
                    table.ForeignKey(
                        name: "FK_BomLines_BomRevisions_BomRevisionId",
                        column: x => x.BomRevisionId,
                        principalTable: "BomRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BomLineDesignators",
                columns: table => new
                {
                    BomLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Designator = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_100_BIN2"),
                    BomRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomLineDesignators", x => new { x.BomLineId, x.Designator });
                    table.ForeignKey(
                        name: "FK_BomLineDesignators_BomLines_BomLineId",
                        column: x => x.BomLineId,
                        principalTable: "BomLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BomLineDesignators_BomRevisions_BomRevisionId",
                        column: x => x.BomRevisionId,
                        principalTable: "BomRevisions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BomLineExtraValues",
                columns: table => new
                {
                    BomLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Header = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Value = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomLineExtraValues", x => new { x.BomLineId, x.Ordinal });
                    table.CheckConstraint("CK_BomLineExtraValues_Ordinal", "[Ordinal] >= 1");
                    table.ForeignKey(
                        name: "FK_BomLineExtraValues_BomLines_BomLineId",
                        column: x => x.BomLineId,
                        principalTable: "BomLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BomLineDesignators_BomRevisionId_Designator",
                table: "BomLineDesignators",
                columns: new[] { "BomRevisionId", "Designator" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BomLines_BomRevisionId_LineNumber",
                table: "BomLines",
                columns: new[] { "BomRevisionId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BomRevisions_PackageId_Number",
                table: "BomRevisions",
                columns: new[] { "PackageId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Packages_CustomerId",
                table: "Packages",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "UX_Packages_CodeRevisionCustomer",
                table: "Packages",
                columns: new[] { "Code", "CustomerRevision", "CustomerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BomLineDesignators");

            migrationBuilder.DropTable(
                name: "BomLineExtraValues");

            migrationBuilder.DropTable(
                name: "BomLines");

            migrationBuilder.DropTable(
                name: "BomRevisions");

            migrationBuilder.DropTable(
                name: "Packages");
        }
    }
}
