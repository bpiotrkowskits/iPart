using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace iPart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialComponentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AecQualifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AecQualifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CasePatterns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Pattern = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasePatterns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Manufacturers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manufacturers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MountingMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MountingMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subcategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subcategories", x => x.Id);
                    table.UniqueConstraint("AK_Subcategories_Id_CategoryId", x => new { x.Id, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_Subcategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Components",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mpn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManufacturerId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    NamePl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NameEng = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DistributorName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomerComponentName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    SubcategoryId = table.Column<int>(type: "int", nullable: true),
                    MountingMethodId = table.Column<int>(type: "int", nullable: true),
                    CaseName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CasePatternId = table.Column<int>(type: "int", nullable: true),
                    Footprint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Msl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Rohs = table.Column<bool>(type: "bit", nullable: true),
                    ProductStatusId = table.Column<int>(type: "int", nullable: true),
                    MinimumOperatingTemperatureC = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    MaximumOperatingTemperatureC = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    Critical = table.Column<bool>(type: "bit", nullable: true),
                    AdditionalDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    LabelDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Components", x => x.Id);
                    table.CheckConstraint("CK_Components_MpnNormalized", "DATALENGTH([Mpn]) > 0 AND UNICODE(LEFT([Mpn], 1)) NOT IN (9,10,11,12,13,32,133,160,5760,8192,8193,8194,8195,8196,8197,8198,8199,8200,8201,8202,8232,8233,8239,8287,12288) AND UNICODE(RIGHT([Mpn], 1)) NOT IN (9,10,11,12,13,32,133,160,5760,8192,8193,8194,8195,8196,8197,8198,8199,8200,8201,8202,8232,8233,8239,8287,12288)");
                    table.CheckConstraint("CK_Components_SubcategoryRequiresCategory", "[SubcategoryId] IS NULL OR [CategoryId] IS NOT NULL");
                    table.CheckConstraint("CK_Components_TemperatureOrder", "[MinimumOperatingTemperatureC] IS NULL OR [MaximumOperatingTemperatureC] IS NULL OR [MinimumOperatingTemperatureC] <= [MaximumOperatingTemperatureC]");
                    table.ForeignKey(
                        name: "FK_Components_CasePatterns_CasePatternId",
                        column: x => x.CasePatternId,
                        principalTable: "CasePatterns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Components_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Components_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Components_Manufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "Manufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Components_MountingMethods_MountingMethodId",
                        column: x => x.MountingMethodId,
                        principalTable: "MountingMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Components_ProductStatuses_ProductStatusId",
                        column: x => x.ProductStatusId,
                        principalTable: "ProductStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Components_Subcategories_SubcategoryId_CategoryId",
                        columns: x => new { x.SubcategoryId, x.CategoryId },
                        principalTable: "Subcategories",
                        principalColumns: new[] { "Id", "CategoryId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CapacitorParameters",
                columns: table => new
                {
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapacitancePf = table.Column<decimal>(type: "decimal(28,9)", precision: 28, scale: 9, nullable: true),
                    RatedVoltageV = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    TolerancePercent = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    AbsoluteTolerancePf = table.Column<decimal>(type: "decimal(28,9)", precision: 28, scale: 9, nullable: true),
                    Dielectric = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EsrMilliOhms = table.Column<decimal>(type: "decimal(28,9)", precision: 28, scale: 9, nullable: true),
                    LowEsr = table.Column<bool>(type: "bit", nullable: true),
                    LifetimeHours = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapacitorParameters", x => x.ComponentId);
                    table.CheckConstraint("CK_Capacitor_Lifetime", "[LifetimeHours] >= 0");
                    table.CheckConstraint("CK_Capacitor_ToleranceAlternative", "[TolerancePercent] IS NULL OR [AbsoluteTolerancePf] IS NULL");
                    table.CheckConstraint("CK_CapacitorParameters_AbsoluteTolerancePf", "[AbsoluteTolerancePf] >= 0");
                    table.CheckConstraint("CK_CapacitorParameters_CapacitancePf", "[CapacitancePf] >= 0");
                    table.CheckConstraint("CK_CapacitorParameters_EsrMilliOhms", "[EsrMilliOhms] >= 0");
                    table.CheckConstraint("CK_CapacitorParameters_RatedVoltageV", "[RatedVoltageV] >= 0");
                    table.CheckConstraint("CK_CapacitorParameters_TolerancePercent", "[TolerancePercent] >= 0");
                    table.ForeignKey(
                        name: "FK_CapacitorParameters_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Operation = table.Column<int>(type: "int", nullable: false),
                    FieldPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PreviousValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComponentHistory_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComponentLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentLinks", x => x.Id);
                    table.CheckConstraint("CK_ComponentLinks_Kind", "[Kind] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_ComponentLinks_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentPads",
                columns: table => new
                {
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RC = table.Column<int>(type: "int", nullable: true),
                    TT = table.Column<int>(type: "int", nullable: true),
                    DL = table.Column<int>(type: "int", nullable: true),
                    QL = table.Column<int>(type: "int", nullable: true),
                    BGA = table.Column<int>(type: "int", nullable: true),
                    TP = table.Column<int>(type: "int", nullable: true),
                    THT = table.Column<int>(type: "int", nullable: true),
                    PreTHT = table.Column<int>(type: "int", nullable: true),
                    PressFIT = table.Column<int>(type: "int", nullable: true),
                    MECH = table.Column<int>(type: "int", nullable: true),
                    BTC = table.Column<bool>(type: "bit", nullable: true),
                    NS = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentPads", x => x.ComponentId);
                    table.CheckConstraint("CK_Pads_BGA", "[BGA] = 0 OR [BGA] >= 3");
                    table.CheckConstraint("CK_Pads_DL", "[DL] = 0 OR [DL] >= 3");
                    table.CheckConstraint("CK_Pads_MECH", "[MECH] >= 0");
                    table.CheckConstraint("CK_Pads_PressFIT", "[PressFIT] >= 0");
                    table.CheckConstraint("CK_Pads_PreTHT", "[PreTHT] >= 0");
                    table.CheckConstraint("CK_Pads_QL", "[QL] = 0 OR [QL] >= 4");
                    table.CheckConstraint("CK_Pads_RC", "[RC] IN (0, 2)");
                    table.CheckConstraint("CK_Pads_THT", "[THT] >= 0");
                    table.CheckConstraint("CK_Pads_TP", "[TP] >= 0");
                    table.CheckConstraint("CK_Pads_TT", "[TT] IN (0, 2)");
                    table.ForeignKey(
                        name: "FK_ComponentPads_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentQualifications",
                columns: table => new
                {
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QualificationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentQualifications", x => new { x.ComponentId, x.QualificationId });
                    table.ForeignKey(
                        name: "FK_ComponentQualifications_AecQualifications_QualificationId",
                        column: x => x.QualificationId,
                        principalTable: "AecQualifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComponentQualifications_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConnectorParameters",
                columns: table => new
                {
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PositionCount = table.Column<int>(type: "int", nullable: true),
                    PitchMm = table.Column<decimal>(type: "decimal(18,9)", precision: 18, scale: 9, nullable: true),
                    RowCount = table.Column<int>(type: "int", nullable: true),
                    CompatiblePlug = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectorParameters", x => x.ComponentId);
                    table.CheckConstraint("CK_Connector_PositionCount", "[PositionCount] >= 0");
                    table.CheckConstraint("CK_Connector_RowCount", "[RowCount] >= 0");
                    table.CheckConstraint("CK_ConnectorParameters_PitchMm", "[PitchMm] >= 0");
                    table.ForeignKey(
                        name: "FK_ConnectorParameters_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrystalParameters",
                columns: table => new
                {
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrequencyHz = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    LoadCapacitancePf = table.Column<decimal>(type: "decimal(28,9)", precision: 28, scale: 9, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrystalParameters", x => x.ComponentId);
                    table.CheckConstraint("CK_CrystalParameters_FrequencyHz", "[FrequencyHz] >= 0");
                    table.CheckConstraint("CK_CrystalParameters_LoadCapacitancePf", "[LoadCapacitancePf] >= 0");
                    table.ForeignKey(
                        name: "FK_CrystalParameters_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InductorParameters",
                columns: table => new
                {
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InductanceMicroHenries = table.Column<decimal>(type: "decimal(28,9)", precision: 28, scale: 9, nullable: true),
                    TolerancePercent = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    RatedCurrentA = table.Column<decimal>(type: "decimal(18,9)", precision: 18, scale: 9, nullable: true),
                    DcrOhms = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: true),
                    Core = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InductorParameters", x => x.ComponentId);
                    table.CheckConstraint("CK_InductorParameters_DcrOhms", "[DcrOhms] >= 0");
                    table.CheckConstraint("CK_InductorParameters_InductanceMicroHenries", "[InductanceMicroHenries] >= 0");
                    table.CheckConstraint("CK_InductorParameters_RatedCurrentA", "[RatedCurrentA] >= 0");
                    table.CheckConstraint("CK_InductorParameters_TolerancePercent", "[TolerancePercent] >= 0");
                    table.ForeignKey(
                        name: "FK_InductorParameters_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LedParameters",
                columns: table => new
                {
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LuminousIntensityCd = table.Column<decimal>(type: "decimal(28,9)", precision: 28, scale: 9, nullable: true),
                    LuminousFluxLm = table.Column<decimal>(type: "decimal(28,9)", precision: 28, scale: 9, nullable: true),
                    ForwardCurrentA = table.Column<decimal>(type: "decimal(18,9)", precision: 18, scale: 9, nullable: true),
                    ForwardVoltageV = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    DominantWavelengthNm = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedParameters", x => x.ComponentId);
                    table.CheckConstraint("CK_LedParameters_DominantWavelengthNm", "[DominantWavelengthNm] >= 0");
                    table.CheckConstraint("CK_LedParameters_ForwardCurrentA", "[ForwardCurrentA] >= 0");
                    table.CheckConstraint("CK_LedParameters_ForwardVoltageV", "[ForwardVoltageV] >= 0");
                    table.CheckConstraint("CK_LedParameters_LuminousFluxLm", "[LuminousFluxLm] >= 0");
                    table.CheckConstraint("CK_LedParameters_LuminousIntensityCd", "[LuminousIntensityCd] >= 0");
                    table.ForeignKey(
                        name: "FK_LedParameters_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResistorParameters",
                columns: table => new
                {
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResistanceOhms = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: true),
                    TolerancePercent = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    PowerWatts = table.Column<decimal>(type: "decimal(18,9)", precision: 18, scale: 9, nullable: true),
                    TemperatureCoefficientPpmPerC = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResistorParameters", x => x.ComponentId);
                    table.CheckConstraint("CK_ResistorParameters_PowerWatts", "[PowerWatts] >= 0");
                    table.CheckConstraint("CK_ResistorParameters_ResistanceOhms", "[ResistanceOhms] >= 0");
                    table.CheckConstraint("CK_ResistorParameters_TolerancePercent", "[TolerancePercent] >= 0");
                    table.ForeignKey(
                        name: "FK_ResistorParameters_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AecQualifications",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "AEC-Q200" },
                    { 2, "AEC-Q101" },
                    { 3, "AEC-Q100" }
                });

            migrationBuilder.InsertData(
                table: "MountingMethods",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "SMT" },
                    { 2, "Pin-In-Paste" },
                    { 3, "THT" },
                    { 4, "Press-Fit" },
                    { 5, "Plugs" },
                    { 6, "Mech" }
                });

            migrationBuilder.InsertData(
                table: "ProductStatuses",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Last Time Buy" },
                    { 2, "Not For New Design" },
                    { 3, "Obsolete" },
                    { 4, "End of Life" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AecQualifications_Name",
                table: "AecQualifications",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CapacitorParameters_CapacitancePf",
                table: "CapacitorParameters",
                column: "CapacitancePf");

            migrationBuilder.CreateIndex(
                name: "IX_CapacitorParameters_RatedVoltageV",
                table: "CapacitorParameters",
                column: "RatedVoltageV");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentHistory_ComponentId_OccurredAtUtc",
                table: "ComponentHistory",
                columns: new[] { "ComponentId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentHistory_OperationId",
                table: "ComponentHistory",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentLinks_ComponentId",
                table: "ComponentLinks",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentQualifications_QualificationId",
                table: "ComponentQualifications",
                column: "QualificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Components_CasePatternId",
                table: "Components",
                column: "CasePatternId");

            migrationBuilder.CreateIndex(
                name: "IX_Components_CategoryId",
                table: "Components",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Components_CustomerId",
                table: "Components",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Components_ManufacturerId",
                table: "Components",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_Components_MountingMethodId",
                table: "Components",
                column: "MountingMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Components_ProductStatusId",
                table: "Components",
                column: "ProductStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Components_SubcategoryId_CategoryId",
                table: "Components",
                columns: new[] { "SubcategoryId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "UX_Components_BusinessKey",
                table: "Components",
                columns: new[] { "Mpn", "ManufacturerId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorParameters_PitchMm",
                table: "ConnectorParameters",
                column: "PitchMm");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorParameters_PositionCount",
                table: "ConnectorParameters",
                column: "PositionCount");

            migrationBuilder.CreateIndex(
                name: "IX_CrystalParameters_FrequencyHz",
                table: "CrystalParameters",
                column: "FrequencyHz");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Name",
                table: "Customers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_InductorParameters_InductanceMicroHenries",
                table: "InductorParameters",
                column: "InductanceMicroHenries");

            migrationBuilder.CreateIndex(
                name: "IX_InductorParameters_RatedCurrentA",
                table: "InductorParameters",
                column: "RatedCurrentA");

            migrationBuilder.CreateIndex(
                name: "IX_LedParameters_Color",
                table: "LedParameters",
                column: "Color");

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturers_Name",
                table: "Manufacturers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_MountingMethods_Name",
                table: "MountingMethods",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ProductStatuses_Name",
                table: "ProductStatuses",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ResistorParameters_PowerWatts",
                table: "ResistorParameters",
                column: "PowerWatts");

            migrationBuilder.CreateIndex(
                name: "IX_ResistorParameters_ResistanceOhms",
                table: "ResistorParameters",
                column: "ResistanceOhms");

            migrationBuilder.CreateIndex(
                name: "IX_ResistorParameters_TolerancePercent",
                table: "ResistorParameters",
                column: "TolerancePercent");

            migrationBuilder.CreateIndex(
                name: "IX_Subcategories_CategoryId",
                table: "Subcategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Subcategories_Name",
                table: "Subcategories",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapacitorParameters");

            migrationBuilder.DropTable(
                name: "ComponentHistory");

            migrationBuilder.DropTable(
                name: "ComponentLinks");

            migrationBuilder.DropTable(
                name: "ComponentPads");

            migrationBuilder.DropTable(
                name: "ComponentQualifications");

            migrationBuilder.DropTable(
                name: "ConnectorParameters");

            migrationBuilder.DropTable(
                name: "CrystalParameters");

            migrationBuilder.DropTable(
                name: "InductorParameters");

            migrationBuilder.DropTable(
                name: "LedParameters");

            migrationBuilder.DropTable(
                name: "ResistorParameters");

            migrationBuilder.DropTable(
                name: "AecQualifications");

            migrationBuilder.DropTable(
                name: "Components");

            migrationBuilder.DropTable(
                name: "CasePatterns");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Manufacturers");

            migrationBuilder.DropTable(
                name: "MountingMethods");

            migrationBuilder.DropTable(
                name: "ProductStatuses");

            migrationBuilder.DropTable(
                name: "Subcategories");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
