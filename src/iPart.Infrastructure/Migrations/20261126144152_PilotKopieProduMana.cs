using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iPart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PilotKopieProduMana : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMembers_Users_UserId",
                schema: "shared",
                table: "GroupMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAccess_Users_UserId",
                schema: "shared",
                table: "UserAccess");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "shared");

            migrationBuilder.DropIndex(
                name: "IX_GroupMembers_UserId",
                schema: "shared",
                table: "GroupMembers");

            // Kopie danych ProduMana na czas pilotażu (ADR-0014): te same nazwy i kolumny co widoki ProduMana,
            // tylko do odczytu, odświeżane w całości na prośbę. Po pilotażu zastąpią je widoki — bez zmian w zapytaniach.
            // EF mapuje je jako widoki, więc tabele tworzymy tutaj SQL-em.
            migrationBuilder.EnsureSchema(name: "produman");
            migrationBuilder.EnsureSchema(name: "employees");
            // Lista kont: 7 kolumn z produman.users. Bez PIN, RFID i QR — nigdy ich nie kopiujemy.
            migrationBuilder.Sql("""
                CREATE TABLE [produman].[users] (
                    [UserId] uniqueidentifier NOT NULL CONSTRAINT [PK_produman_users] PRIMARY KEY,
                    [Login] nvarchar(256) NULL,
                    [FullName] nvarchar(256) NULL,
                    [Email] nvarchar(256) NULL,
                    [Department] int NULL,
                    [IsActive] bit NOT NULL,
                    [Deleted] bit NOT NULL
                );
                """);
            // Wspólny słownik działów (employees.Departments). Działów się nie usuwa, tylko wyłącza.
            migrationBuilder.Sql("""
                CREATE TABLE [employees].[Departments] (
                    [Id] int NOT NULL CONSTRAINT [PK_employees_Departments] PRIMARY KEY,
                    [Code] nvarchar(20) NOT NULL,
                    [Name] nvarchar(100) NOT NULL,
                    [IsActive] bit NOT NULL,
                    [SortOrder] int NOT NULL
                );
                """);
            // Dane startowe: lista początkowa z ADR-0036 ProduMana, pkt 6. Odświeżenie kopią nadpisuje całość.
            migrationBuilder.Sql("""
                INSERT INTO [employees].[Departments] ([Id], [Code], [Name], [IsActive], [SortOrder]) VALUES
                (100100, N'MG', N'Magazyn', 1, 10), (100101, N'SMT', N'SMT', 1, 20), (100102, N'THT', N'THT', 1, 30),
                (100103, N'KJ', N'Kontrola jakości', 1, 40), (100104, N'TSL', N'TS Labs', 1, 50), (100105, N'SZ', N'Szablony', 1, 60),
                (100106, N'RD', N'R&D', 1, 70), (100107, N'IT', N'IT', 1, 80), (100108, N'ZAK', N'Zakupy', 1, 90),
                (100109, N'OP', N'Opiekunowie', 1, 100), (100111, N'WDR', N'Wdrożenia', 0, 110), (100113, N'DOK', N'Dokumentacja', 1, 120),
                (200001, N'JAK', N'Jakość', 1, 130), (200002, N'TECH', N'Dział techniczny', 1, 140), (200003, N'ZAR', N'Zarząd', 1, 150),
                (200004, N'ADM', N'Administracja', 1, 160), (200005, N'PROD', N'Kierownictwo produkcji', 1, 170);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE [employees].[Departments];");
            migrationBuilder.Sql("DROP TABLE [produman].[users];");
            migrationBuilder.Sql("DROP SCHEMA [employees];");
            migrationBuilder.Sql("DROP SCHEMA [produman];");

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FirstLoginAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastLoginAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Login = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_UserId",
                schema: "shared",
                table: "GroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login",
                schema: "shared",
                table: "Users",
                column: "Login");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMembers_Users_UserId",
                schema: "shared",
                table: "GroupMembers",
                column: "UserId",
                principalSchema: "shared",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccess_Users_UserId",
                schema: "shared",
                table: "UserAccess",
                column: "UserId",
                principalSchema: "shared",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
