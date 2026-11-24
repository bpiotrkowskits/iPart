using iPart.Core.Components;
using iPart.Core.Packages;
using iPart.Infrastructure;
using iPart.Infrastructure.Packages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
        await using var configured = new IPartDbContextFactory().CreateDbContext(["--settings", Path.Combine(directory!.FullName, "src/iPart.App/appsettings.json")]);
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
            var first = await service.SaveBomRevisionAsync(packageId, import, @"C:\x\bom.xlsx", "BOM", null);
            var second = await service.SaveBomRevisionAsync(packageId, import, "bom2.xlsx", "BOM", "druga");
            Assert.Equal((1, 2), (first.Number, second.Number));

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

    private sealed class FixedUser : ICurrentUser { public string GetAuthor() => "TEST\\user"; }
}
