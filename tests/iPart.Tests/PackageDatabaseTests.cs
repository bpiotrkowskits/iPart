using iPart.Core.Components;
using iPart.Core.Shared;
using iPart.Core.Packages;
using iPart.Infrastructure;
using iPart.Infrastructure.Packages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace iPart.Tests;

// Próba generalna wdrożenia: tworzy NOWĄ, tymczasową bazę na serwerze z appsettings.json,
// stosuje do niej wszystkie migracje (tak jak administrator na serwerze), sprawdza serwis pakietów i usuwa bazę.
// Baza dataPart nie jest dotykana. Włączenie: IPART_RUN_SQL_TESTS=1.
public sealed class PackageDatabaseTests
{
    [SqlServerFact]
    public async Task MigrationsApplyToEmptyDatabaseAndPackageRoundTripWorks()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "iPart.slnx"))) directory = directory.Parent;
        await using var configured = new IPartDbContextFactory().CreateDbContext(["--settings", TestSettings.Path(directory!.FullName)]);
        var builder = new SqlConnectionStringBuilder(configured.Database.GetConnectionString())
        {
            InitialCatalog = "iPart_Verify_" + Guid.NewGuid().ToString("N")[..12],
            Pooling = false
        };
        var options = new DbContextOptionsBuilder<IPartDbContext>().UseSqlServer(builder.ConnectionString).Options;
        var contexts = new IPartContextFactory(options);
        try
        {
            await using (var context = contexts.CreateDbContext())
            {
                await context.Database.MigrateAsync();
                Assert.Empty(await context.Database.GetPendingMigrationsAsync());
            }

            var service = new SqlServerPackageService(contexts, new FixedUser());
            var customerId = await service.AddCustomerAsync("K1");
            var packageId = await service.CreatePackageAsync(new NewPackageRequest(" PCBA1 ", "C", customerId, null));
            await Assert.ThrowsAsync<PackageOperationException>(() => service.CreatePackageAsync(new NewPackageRequest("PCBA1", "C", customerId, null)));

            var table = new BomSourceTable(["Des", "Qty", "PN", "Extra"], [
                new BomSourceRow(2, ["R1-R3", "3", "RC0402", "uwaga"]),
                new BomSourceRow(3, ["U1", "1", "STM32", null])]);
            var import = BomImportBuilder.Build(table, [new(0, BomField.Designator), new(1, BomField.Quantity), new(2, BomField.PartNumber)]);
            var first = await service.SaveBomRevisionAsync(packageId, import, @"C:\x\bom.xlsx", "BOM", null, [1, 2, 3]);
            var second = await service.SaveBomRevisionAsync(packageId, import, "bom2.xlsx", "BOM", "druga", [4, 5]);
            Assert.Equal((1, 2), (first.Number, second.Number));

            // Kopia pliku w bazie (ADR-0008) i wpisy historii obszaru (ADR-0007).
            await using (var check = contexts.CreateDbContext())
            {
                var revision = await check.BomRevisions.SingleAsync(r => r.Id == first.Id);
                var file = await check.DocumentationFiles.SingleAsync(f => f.Id == revision.SourceFileId);
                Assert.Equal(("bom.xlsx", 3L), (file.FileName, file.SizeBytes));
                Assert.Equal(new byte[] { 1, 2, 3 }, file.Content);
                Assert.Equal(StoredFile.ComputeSha256([1, 2, 3]), file.Sha256);
                Assert.Equal(3, await check.DocumentationChangeSets.CountAsync(c => c.SubjectId == packageId));
                Assert.Equal(1, await check.SharedChangeSets.CountAsync(c => c.SubjectId == customerId));
            }

            var packages = await service.ListPackagesAsync("PCBA");
            var package = Assert.Single(packages);
            Assert.Equal(("PCBA1", "K1", 2), (package.Code, package.CustomerName, package.RevisionCount));
            var lines = await service.GetLinesAsync(first.Id);
            Assert.Equal(new[] { 1, 2 }, lines.Select(l => l.LineNumber));
            Assert.Equal(3, lines[0].ExpandedDesignators.Count);
            Assert.Equal("uwaga", Assert.Single(lines[0].ExtraValues).Value);
            Assert.Equal("bom.xlsx", first.SourceFileName);

            var info = await new SqlServerDatabaseInfoReader(builder.ConnectionString, contexts).ReadAsync();
            Assert.Empty(info.PendingMigrations);
            Assert.Equal(builder.InitialCatalog, info.Database);
        }
        finally
        {
            await using var context = contexts.CreateDbContext();
            await context.Database.EnsureDeletedAsync();
        }
    }

    // Aktualizacja bazy 0.1.0 z danymi do etapu 1: Klienci dostają GUID i zostają przy pakietach,
    // historia komponentów przechodzi do wspólnej budowy, schemat dataPart znika.
    [SqlServerFact]
    public async Task Stage1MigrationKeepsCustomersPackageLinksAndComponentHistory()
    {
        var (contexts, catalog) = TemporaryDatabase();
        try
        {
            await using (var context = contexts.CreateDbContext())
            {
                await context.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().MigrateAsync("MoveTablesToDataPartSchema");
                await context.Database.ExecuteSqlRawAsync("""
                    INSERT dataPart.Customers(Name) VALUES (N'Klient A'), (N'Klient B');
                    DECLARE @a int = (SELECT Id FROM dataPart.Customers WHERE Name = N'Klient A');
                    DECLARE @c uniqueidentifier = NEWID(), @p uniqueidentifier = NEWID(), @op uniqueidentifier = NEWID();
                    INSERT dataPart.Components(Id, Mpn, Version, CustomerId) VALUES (@c, N'RC0402', NEWID(), @a);
                    INSERT dataPart.ComponentHistory(OperationId, ComponentId, Author, OccurredAtUtc, Operation, FieldPath, PreviousValue, NewValue)
                    VALUES (@op, @c, N'TS\u1', SYSDATETIMEOFFSET(), 1, N'Component.Exists', NULL, N'true'),
                           (@op, @c, N'TS\u1', SYSDATETIMEOFFSET(), 1, N'Mpn', NULL, N'"RC0402"');
                    INSERT dataPart.Packages(Id, Code, CustomerRevision, CustomerId, CreatedAtUtc, CreatedBy)
                    VALUES (@p, N'PCBA1', N'C', @a, SYSDATETIMEOFFSET(), N'TS\u1'), (NEWID(), N'PCBA2', N'A', NULL, SYSDATETIMEOFFSET(), N'TS\u1');
                    """);
                await context.Database.MigrateAsync();
                Assert.False(context.Database.HasPendingModelChanges());
            }

            await using (var context = contexts.CreateDbContext())
            {
                var customers = await context.Customers.OrderBy(c => c.Name).ToListAsync();
                Assert.Equal(new[] { "Klient A", "Klient B" }, customers.Select(c => c.Name));
                var packages = await context.Packages.OrderBy(p => p.Code).ToListAsync();
                Assert.Equal(customers[0].Id, packages[0].CustomerId);
                Assert.Null(packages[1].CustomerId);
                var set = Assert.Single(await context.ComponentChangeSets.ToListAsync());
                Assert.Equal((ChangeOperations.Created, "TS\\u1"), (set.Operation, set.Author));
                Assert.Equal(2, await context.ComponentFieldChanges.CountAsync(f => f.ChangeSetId == set.Id));
                Assert.Equal(8, await context.MountingMethods.CountAsync());
                Assert.Equal(17, await context.Departments.CountAsync());   // słownik działów z ADR-0036 ProduMana
                Assert.Equal(0, await context.ProduManAccounts.CountAsync()); // kopia kont — wgrywana osobno
                Assert.Equal(0, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.schemas WHERE name = N'dataPart'").SingleAsync());
            }
        }
        finally
        {
            await using var context = contexts.CreateDbContext();
            await context.Database.EnsureDeletedAsync();
        }
        Assert.StartsWith("iPart_Verify_", catalog);
    }

    // Ten sam MPN i producent u dwóch Klientów — migracja zatrzymuje się przed zmianami.
    [SqlServerFact]
    public async Task Stage1MigrationStopsWhenSameMpnAndManufacturerBelongsToTwoCustomers()
    {
        var (contexts, _) = TemporaryDatabase();
        try
        {
            await using var context = contexts.CreateDbContext();
            await context.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().MigrateAsync("MoveTablesToDataPartSchema");
            await context.Database.ExecuteSqlRawAsync("""
                INSERT dataPart.Customers(Name) VALUES (N'Klient A'), (N'Klient B');
                INSERT dataPart.Components(Id, Mpn, Version, CustomerId) SELECT NEWID(), N'RC0402', NEWID(), Id FROM dataPart.Customers;
                """);
            var error = await Assert.ThrowsAsync<SqlException>(() => context.Database.MigrateAsync());
            Assert.Equal(50001, error.Number);
            Assert.Contains("Etap1Fundament", string.Join(",", await context.Database.GetPendingMigrationsAsync()));
        }
        finally
        {
            await using var context = contexts.CreateDbContext();
            await context.Database.EnsureDeletedAsync();
        }
    }

    private static (IPartContextFactory Contexts, string Catalog) TemporaryDatabase()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "iPart.slnx"))) directory = directory.Parent;
        using var configured = new IPartDbContextFactory().CreateDbContext(["--settings", TestSettings.Path(directory!.FullName)]);
        var builder = new SqlConnectionStringBuilder(configured.Database.GetConnectionString())
        {
            InitialCatalog = "iPart_Verify_" + Guid.NewGuid().ToString("N")[..12],
            Pooling = false
        };
        return (new IPartContextFactory(new DbContextOptionsBuilder<IPartDbContext>().UseSqlServer(builder.ConnectionString).Options), builder.InitialCatalog);
    }

    private sealed class FixedUser : ICurrentUser { public string GetAuthor() => "TEST\\user"; }
}
