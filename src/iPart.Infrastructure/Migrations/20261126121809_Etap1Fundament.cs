using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace iPart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Etap1Fundament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Etap 1 (ADR-0005, ADR-0006, ADR-0007, ADR-0008): schematy obszarów, Klient z GUID,
            // klucz komponentu MPN + producent, wspólna historia, pliki w bazie, użytkownicy i grupy.
            // Część operacji EF zastąpiono SQL, żeby zachować dane (Klienci, powiązania pakietów, historia komponentów).

            // Klucz komponentu traci Klienta (10.5). Jeśli ten sam MPN i producent występuje u kilku Klientów,
            // migracja zatrzymuje się, zanim cokolwiek zmieni — rekordy trzeba najpierw scalić ręcznie.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [dataPart].[Components] GROUP BY [Mpn], [ManufacturerId] HAVING COUNT(*) > 1)
                    THROW 50001, N'Etap 1: w [dataPart].[Components] ten sam MPN i producent występuje kilka razy (różni Klienci). Scal te rekordy przed migracją.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Components_Customers_CustomerId",
                schema: "dataPart",
                table: "Components");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Name",
                schema: "dataPart",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Components_CustomerId",
                schema: "dataPart",
                table: "Components");

            migrationBuilder.DropIndex(
                name: "UX_Components_BusinessKey",
                schema: "dataPart",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                schema: "dataPart",
                table: "Components");

            migrationBuilder.EnsureSchema(
                name: "components");

            migrationBuilder.EnsureSchema(
                name: "documentation");

            migrationBuilder.EnsureSchema(
                name: "shared");

            migrationBuilder.RenameTable(
                name: "Subcategories",
                schema: "dataPart",
                newName: "Subcategories",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "ResistorParameters",
                schema: "dataPart",
                newName: "ResistorParameters",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "ProductStatuses",
                schema: "dataPart",
                newName: "ProductStatuses",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "Packages",
                schema: "dataPart",
                newName: "Packages",
                newSchema: "documentation");

            migrationBuilder.RenameTable(
                name: "MountingMethods",
                schema: "dataPart",
                newName: "MountingMethods",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "Manufacturers",
                schema: "dataPart",
                newName: "Manufacturers",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "LedParameters",
                schema: "dataPart",
                newName: "LedParameters",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "InductorParameters",
                schema: "dataPart",
                newName: "InductorParameters",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "Customers",
                schema: "dataPart",
                newName: "Customers",
                newSchema: "shared");

            migrationBuilder.RenameTable(
                name: "CrystalParameters",
                schema: "dataPart",
                newName: "CrystalParameters",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "ConnectorParameters",
                schema: "dataPart",
                newName: "ConnectorParameters",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "Components",
                schema: "dataPart",
                newName: "Components",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "ComponentQualifications",
                schema: "dataPart",
                newName: "ComponentQualifications",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "ComponentPads",
                schema: "dataPart",
                newName: "ComponentPads",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "ComponentLinks",
                schema: "dataPart",
                newName: "ComponentLinks",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "Categories",
                schema: "dataPart",
                newName: "Categories",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "CasePatterns",
                schema: "dataPart",
                newName: "CasePatterns",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "CapacitorParameters",
                schema: "dataPart",
                newName: "CapacitorParameters",
                newSchema: "components");

            migrationBuilder.RenameTable(
                name: "BomRevisions",
                schema: "dataPart",
                newName: "BomRevisions",
                newSchema: "documentation");

            migrationBuilder.RenameTable(
                name: "BomLines",
                schema: "dataPart",
                newName: "BomLines",
                newSchema: "documentation");

            migrationBuilder.RenameTable(
                name: "BomLineExtraValues",
                schema: "dataPart",
                newName: "BomLineExtraValues",
                newSchema: "documentation");

            migrationBuilder.RenameTable(
                name: "BomLineDesignators",
                schema: "dataPart",
                newName: "BomLineDesignators",
                newSchema: "documentation");

            migrationBuilder.RenameTable(
                name: "AecQualifications",
                schema: "dataPart",
                newName: "AecQualifications",
                newSchema: "components");

            // Klient: int (IDENTITY) → GUID (ADR-0006), z zachowaniem przypisań pakietów.
            // Osobne wywołania Sql — kolumna dodana w jednym wsadzie nie jest widoczna przy kompilacji tego samego wsadu.
            migrationBuilder.DropForeignKey(name: "FK_Packages_Customers_CustomerId", schema: "documentation", table: "Packages");
            migrationBuilder.DropIndex(name: "UX_Packages_CodeRevisionCustomer", schema: "documentation", table: "Packages");
            migrationBuilder.DropIndex(name: "IX_Packages_CustomerId", schema: "documentation", table: "Packages");
            migrationBuilder.Sql("ALTER TABLE [shared].[Customers] ADD [NewId] uniqueidentifier NOT NULL CONSTRAINT [DF_Customers_NewId] DEFAULT NEWID();");
            migrationBuilder.Sql("ALTER TABLE [documentation].[Packages] ADD [NewCustomerId] uniqueidentifier NULL;");
            // EXEC — skrypt idempotentny kompiluje cały wsad także na bazie już zaktualizowanej, gdzie tych kolumn nie ma.
            migrationBuilder.Sql("""
                EXEC(N'UPDATE p SET p.[NewCustomerId] = c.[NewId]
                FROM [documentation].[Packages] p JOIN [shared].[Customers] c ON c.[Id] = p.[CustomerId];');
                """);
            migrationBuilder.Sql("ALTER TABLE [documentation].[Packages] DROP COLUMN [CustomerId];");
            migrationBuilder.Sql("EXEC sp_rename N'[documentation].[Packages].[NewCustomerId]', N'CustomerId', N'COLUMN';");
            migrationBuilder.Sql("ALTER TABLE [shared].[Customers] DROP CONSTRAINT [PK_Customers];");
            migrationBuilder.Sql("ALTER TABLE [shared].[Customers] DROP CONSTRAINT [DF_Customers_NewId];");
            migrationBuilder.Sql("ALTER TABLE [shared].[Customers] DROP COLUMN [Id];");
            migrationBuilder.Sql("EXEC sp_rename N'[shared].[Customers].[NewId]', N'Id', N'COLUMN';");
            migrationBuilder.Sql("ALTER TABLE [shared].[Customers] ADD CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]);");
            migrationBuilder.CreateIndex(name: "IX_Packages_CustomerId", schema: "documentation", table: "Packages", column: "CustomerId");
            migrationBuilder.Sql("CREATE UNIQUE INDEX [UX_Packages_CodeRevisionCustomer] ON [documentation].[Packages] ([Code], [CustomerRevision], [CustomerId]);");
            migrationBuilder.AddForeignKey(name: "FK_Packages_Customers_CustomerId", schema: "documentation", table: "Packages",
                column: "CustomerId", principalSchema: "shared", principalTable: "Customers", principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "components",
                table: "MountingMethods",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceFileId",
                schema: "documentation",
                table: "BomRevisions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChangeSets",
                schema: "components",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Author = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubjectType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeSets",
                schema: "documentation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Author = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubjectType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeSets",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Author = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubjectType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                schema: "documentation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UploadedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                    table.CheckConstraint("CK_Files_Size", "[SizeBytes] >= 0 AND [SizeBytes] = DATALENGTH([Content])");
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldChanges",
                schema: "components",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChangeSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Field = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PreviousValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldChanges_ChangeSets_ChangeSetId",
                        column: x => x.ChangeSetId,
                        principalSchema: "components",
                        principalTable: "ChangeSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldChanges",
                schema: "documentation",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChangeSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Field = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PreviousValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldChanges_ChangeSets_ChangeSetId",
                        column: x => x.ChangeSetId,
                        principalSchema: "documentation",
                        principalTable: "ChangeSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldChanges",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChangeSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Field = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PreviousValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldChanges_ChangeSets_ChangeSetId",
                        column: x => x.ChangeSetId,
                        principalSchema: "shared",
                        principalTable: "ChangeSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                schema: "shared",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAdministrator = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => new { x.GroupId, x.UserId });
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "shared",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "shared",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "shared",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleCode });
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "shared",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "shared",
                table: "Groups",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { new Guid("6f1c0b2e-0001-4d6a-9a51-6b1f0c2a0001"), "OBSLUGA_PROJEKTU", "Obsługa Projektu" },
                    { new Guid("6f1c0b2e-0002-4d6a-9a51-6b1f0c2a0002"), "TECHNOLOGIA_SMT", "Technologia SMT" },
                    { new Guid("6f1c0b2e-0003-4d6a-9a51-6b1f0c2a0003"), "ZAKUPY", "Zakupy" },
                    { new Guid("6f1c0b2e-0004-4d6a-9a51-6b1f0c2a0004"), "MAGAZYN", "Magazyn" },
                    { new Guid("6f1c0b2e-0005-4d6a-9a51-6b1f0c2a0005"), "PRODUKCJA", "Produkcja" },
                    { new Guid("6f1c0b2e-0006-4d6a-9a51-6b1f0c2a0006"), "OPIEKUNOWIE_KLIENTA", "Opiekunowie klienta" }
                });

            migrationBuilder.UpdateData(
                schema: "components",
                table: "MountingMethods",
                keyColumn: "Id",
                keyValue: 1,
                column: "Code",
                value: "SMT");

            migrationBuilder.UpdateData(
                schema: "components",
                table: "MountingMethods",
                keyColumn: "Id",
                keyValue: 2,
                column: "Code",
                value: "PIN_IN_PASTE");

            migrationBuilder.UpdateData(
                schema: "components",
                table: "MountingMethods",
                keyColumn: "Id",
                keyValue: 3,
                column: "Code",
                value: "THT");

            migrationBuilder.UpdateData(
                schema: "components",
                table: "MountingMethods",
                keyColumn: "Id",
                keyValue: 4,
                column: "Code",
                value: "PRESS_FIT");

            migrationBuilder.UpdateData(
                schema: "components",
                table: "MountingMethods",
                keyColumn: "Id",
                keyValue: 5,
                column: "Code",
                value: "PLUGS");

            migrationBuilder.UpdateData(
                schema: "components",
                table: "MountingMethods",
                keyColumn: "Id",
                keyValue: 6,
                column: "Code",
                value: "MECH");

            migrationBuilder.InsertData(
                schema: "components",
                table: "MountingMethods",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { 7, "PCB", "PCB" },
                    { 8, "DNP", "DNP" }
                });

            // Sposoby montażu dopisane ręcznie poza słownikiem startowym dostają kod tymczasowy — do poprawy w panelu administratora.
            migrationBuilder.Sql("UPDATE [components].[MountingMethods] SET [Code] = CONCAT(N'M', [Id]) WHERE [Code] = N'';");

            migrationBuilder.CreateIndex(
                name: "IX_MountingMethods_Code",
                schema: "components",
                table: "MountingMethods",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Name",
                schema: "shared",
                table: "Customers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Components_BusinessKey",
                schema: "components",
                table: "Components",
                columns: new[] { "Mpn", "ManufacturerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BomRevisions_SourceFileId",
                schema: "documentation",
                table: "BomRevisions",
                column: "SourceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeSets_SubjectType_SubjectId_OccurredAtUtc",
                schema: "components",
                table: "ChangeSets",
                columns: new[] { "SubjectType", "SubjectId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChangeSets_SubjectType_SubjectId_OccurredAtUtc",
                schema: "documentation",
                table: "ChangeSets",
                columns: new[] { "SubjectType", "SubjectId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChangeSets_SubjectType_SubjectId_OccurredAtUtc",
                schema: "shared",
                table: "ChangeSets",
                columns: new[] { "SubjectType", "SubjectId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldChanges_ChangeSetId",
                schema: "components",
                table: "FieldChanges",
                column: "ChangeSetId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldChanges_ObjectType_ObjectId",
                schema: "components",
                table: "FieldChanges",
                columns: new[] { "ObjectType", "ObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldChanges_ChangeSetId",
                schema: "documentation",
                table: "FieldChanges",
                column: "ChangeSetId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldChanges_ObjectType_ObjectId",
                schema: "documentation",
                table: "FieldChanges",
                columns: new[] { "ObjectType", "ObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldChanges_ChangeSetId",
                schema: "shared",
                table: "FieldChanges",
                column: "ChangeSetId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldChanges_ObjectType_ObjectId",
                schema: "shared",
                table: "FieldChanges",
                columns: new[] { "ObjectType", "ObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Files_Sha256",
                schema: "documentation",
                table: "Files",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_UserId",
                schema: "shared",
                table: "GroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Code",
                schema: "shared",
                table: "Groups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_AccountName",
                schema: "shared",
                table: "Users",
                column: "AccountName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BomRevisions_Files_SourceFileId",
                schema: "documentation",
                table: "BomRevisions",
                column: "SourceFileId",
                principalSchema: "documentation",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Historia komponentów z 0.1.0 przechodzi na wspólny mechanizm (ADR-0007, konsekwencje):
            // jedna operacja (OperationId) = jeden zestaw zmian, każdy wiersz = zmiana pola.
            migrationBuilder.Sql("""
                INSERT INTO [components].[ChangeSets] ([Id], [UserId], [Author], [OccurredAtUtc], [SubjectType], [SubjectId], [Operation], [Comment])
                SELECT h.[OperationId], NULL, MIN(h.[Author]), MIN(h.[OccurredAtUtc]), N'Component', MIN(h.[ComponentId]),
                       CASE MIN(h.[Operation]) WHEN 1 THEN N'Created' ELSE N'Updated' END, NULL
                FROM [dataPart].[ComponentHistory] h
                GROUP BY h.[OperationId];
                """);
            migrationBuilder.Sql("""
                INSERT INTO [components].[FieldChanges] ([ChangeSetId], [ObjectType], [ObjectId], [Field], [PreviousValue], [NewValue])
                SELECT h.[OperationId], N'Component', h.[ComponentId], h.[FieldPath], h.[PreviousValue], h.[NewValue]
                FROM [dataPart].[ComponentHistory] h
                ORDER BY h.[Id];
                """);
            migrationBuilder.DropTable(name: "ComponentHistory", schema: "dataPart");
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.objects o JOIN sys.schemas s ON s.schema_id = o.schema_id WHERE s.name = N'dataPart')
                    DROP SCHEMA [dataPart];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Zmiana nieodwracalna bez utraty danych (GUID Klientów, scalona historia). Cofnięcie — z kopii zapasowej bazy.
            throw new NotSupportedException("Migracji Etap1Fundament nie cofa się skryptem. Przywróć kopię zapasową bazy sprzed wdrożenia.");
        }
    }
}
