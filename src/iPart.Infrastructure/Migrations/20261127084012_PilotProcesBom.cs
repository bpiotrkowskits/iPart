using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iPart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PilotProcesBom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PnpApprovedAtUtc",
                schema: "documentation",
                table: "BomRevisions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PnpApprovedBy",
                schema: "documentation",
                table: "BomRevisions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PnpApprovedByUserId",
                schema: "documentation",
                table: "BomRevisions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PnpFileId",
                schema: "documentation",
                table: "BomRevisions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PnpFileName",
                schema: "documentation",
                table: "BomRevisions",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComparisonDecision",
                schema: "documentation",
                table: "BomLines",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAnswer",
                schema: "documentation",
                table: "BomLines",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DifferenceQuestion",
                schema: "documentation",
                table: "BomLines",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MountingCode",
                schema: "documentation",
                table: "BomLines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BomVersions",
                schema: "documentation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    BomRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LinesJson = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: false),
                    LineCount = table.Column<int>(type: "int", nullable: false),
                    UnconfirmedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomVersions", x => x.Id);
                    table.CheckConstraint("CK_BomVersions_Kind", "[Kind] IN ('BAZOWA', 'DLA_ZK') AND ([Kind] = 'BAZOWA' OR [OrderNumber] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_BomVersions_BomRevisions_BomRevisionId",
                        column: x => x.BomRevisionId,
                        principalSchema: "documentation",
                        principalTable: "BomRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BomVersions_Packages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "documentation",
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LineMpns",
                schema: "documentation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BomLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mpn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Forbidden = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AddedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AddedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineMpns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineMpns_BomLines_BomLineId",
                        column: x => x.BomLineId,
                        principalSchema: "documentation",
                        principalTable: "BomLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PnpRows",
                schema: "documentation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BomRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceRow = table.Column<int>(type: "int", nullable: false),
                    Designator = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    X = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Y = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Rotation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Side = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Footprint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PnpRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PnpRows_BomRevisions_BomRevisionId",
                        column: x => x.BomRevisionId,
                        principalSchema: "documentation",
                        principalTable: "BomRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BomRevisions_PnpFileId",
                schema: "documentation",
                table: "BomRevisions",
                column: "PnpFileId");

            migrationBuilder.CreateIndex(
                name: "IX_BomVersions_BomRevisionId",
                schema: "documentation",
                table: "BomVersions",
                column: "BomRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_BomVersions_PackageId_Number",
                schema: "documentation",
                table: "BomVersions",
                columns: new[] { "PackageId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineMpns_BomLineId_Mpn_Forbidden",
                schema: "documentation",
                table: "LineMpns",
                columns: new[] { "BomLineId", "Mpn", "Forbidden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PnpRows_BomRevisionId_Designator",
                schema: "documentation",
                table: "PnpRows",
                columns: new[] { "BomRevisionId", "Designator" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BomRevisions_Files_PnpFileId",
                schema: "documentation",
                table: "BomRevisions",
                column: "PnpFileId",
                principalSchema: "documentation",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BomRevisions_Files_PnpFileId",
                schema: "documentation",
                table: "BomRevisions");

            migrationBuilder.DropTable(
                name: "BomVersions",
                schema: "documentation");

            migrationBuilder.DropTable(
                name: "LineMpns",
                schema: "documentation");

            migrationBuilder.DropTable(
                name: "PnpRows",
                schema: "documentation");

            migrationBuilder.DropIndex(
                name: "IX_BomRevisions_PnpFileId",
                schema: "documentation",
                table: "BomRevisions");

            migrationBuilder.DropColumn(
                name: "PnpApprovedAtUtc",
                schema: "documentation",
                table: "BomRevisions");

            migrationBuilder.DropColumn(
                name: "PnpApprovedBy",
                schema: "documentation",
                table: "BomRevisions");

            migrationBuilder.DropColumn(
                name: "PnpApprovedByUserId",
                schema: "documentation",
                table: "BomRevisions");

            migrationBuilder.DropColumn(
                name: "PnpFileId",
                schema: "documentation",
                table: "BomRevisions");

            migrationBuilder.DropColumn(
                name: "PnpFileName",
                schema: "documentation",
                table: "BomRevisions");

            migrationBuilder.DropColumn(
                name: "ComparisonDecision",
                schema: "documentation",
                table: "BomLines");

            migrationBuilder.DropColumn(
                name: "CustomerAnswer",
                schema: "documentation",
                table: "BomLines");

            migrationBuilder.DropColumn(
                name: "DifferenceQuestion",
                schema: "documentation",
                table: "BomLines");

            migrationBuilder.DropColumn(
                name: "MountingCode",
                schema: "documentation",
                table: "BomLines");
        }
    }
}
