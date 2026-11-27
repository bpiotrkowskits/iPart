using System.Text.Json;
using iPart.Core.Packages;
using iPart.Core.Shared;
using iPart.Infrastructure;
using iPart.Infrastructure.Packages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace iPart.Tests;

// Cały proces comparator + iPart na nowej, tymczasowej bazie: import BOM i P&P, porównanie, Wykaz Różnic,
// potwierdzenie, sekcja TS, MPN, zatwierdzenie P&P i BOM. Włączenie: IPART_RUN_SQL_TESTS=1.
public sealed class WorkflowDatabaseTests
{
    [SqlServerFact]
    public async Task FullComparatorAndBomProcessCreatesImmutableVersionWithHistory()
    {
        var contexts = TemporaryContexts();
        try
        {
            await using (var context = contexts.CreateDbContext()) await context.Database.MigrateAsync();
            var user = new Session();
            user.SignIn(new ProduManAccount { UserId = Guid.NewGuid(), FullName = "Opiekun (test)", IsActive = true });
            var service = new SqlServerPackageService(contexts, user);
            var reader = new SqlBomWorkflowReader(contexts);

            var packageId = await service.CreatePackageAsync(new NewPackageRequest("PCBA-WF", "A", null, null));
            var table = new BomSourceTable(["Des", "Qty", "PN", "FP"], [
                new BomSourceRow(2, ["R1-R2", "2", "RC0402; ERJ-2", "0402"]),
                new BomSourceRow(3, ["J4", "1", "CONN-6", null])]);
            var import = BomImportBuilder.Build(table, [new(0, BomField.Designator), new(1, BomField.Quantity), new(2, BomField.PartNumber), new(3, BomField.Footprint)]);
            var revision = await service.SaveBomRevisionAsync(packageId, import, "bom.xlsx", "BOM", null, [1, 2, 3]);
            var lines = await service.GetLinesAsync(revision.Id);
            var mpns = await reader.MpnsAsync(revision.Id);
            Assert.Equal(3, mpns.Count);
            Assert.Equal(MpnBases.FromCustomerBomPreferred, mpns.Single(m => m.Mpn == "RC0402").Note);

            // P&P bez J4 → rozbieżność „Brak w P&P”.
            var pnpRows = new List<PnpRow> { new() { Designator = "R1", Footprint = "0402" }, new() { Designator = "R2", Footprint = "0402" } };
            Ok(await new AttachPnpHandler(contexts, user).HandleAsync(new AttachPnp(revision.Id, "pnp.txt", [9, 9], pnpRows)));
            var comparison = BomPnpComparer.Compare(lines, await reader.PnpRowsAsync(revision.Id));
            var j4 = lines.Single(l => l.LineNumber == 2);
            Assert.Contains(comparison.Lines.Single(l => l.LineId == j4.Id).Differences, d => d.Kind == "Brak w P&P");

            // Wykaz Różnic → odpowiedź Klienta → potwierdzenie z podstawą.
            var decide = new SetComparisonDecisionHandler(contexts, user);
            Assert.False((await decide.HandleAsync(new SetComparisonDecision(j4.Id, ComparisonDecisions.DifferenceList, " "))).IsSuccess);
            Ok(await decide.HandleAsync(new SetComparisonDecision(j4.Id, ComparisonDecisions.DifferenceList, "Czy J4 jest montowane (THT)?")));
            Ok(await new RecordCustomerAnswerHandler(contexts, user).HandleAsync(new RecordCustomerAnswer(j4.Id, "Tak, THT ręcznie.")));
            Ok(await new ConfirmBomLineHandler(contexts, user).HandleAsync(new ConfirmBomLine(j4.Id, ConfirmationBases.CustomerAnswer, "mail 27.11")));

            // Sekcja TS i MPN niedozwolony; zatwierdzenie zablokowanego MPN niemożliwe.
            Ok(await new UpdateLineTsHandler(contexts, user).HandleAsync(new UpdateLineTs(j4.Id, "CONN 6P THT", "THT", null)));
            Assert.False((await new UpdateLineTsHandler(contexts, user).HandleAsync(new UpdateLineTs(j4.Id, null, "NIEZNANY", null))).IsSuccess);
            var r = lines.Single(l => l.LineNumber == 1);
            Ok(await new AddLineMpnHandler(contexts, user).HandleAsync(new AddLineMpn(r.Id, "BAD-1", true, "pękające w PP2")));
            Assert.False((await new AddLineMpnHandler(contexts, user).HandleAsync(new AddLineMpn(r.Id, "BAD-1", false, "z parametrów"))).IsSuccess);

            Ok(await new ApprovePnpHandler(contexts, user).HandleAsync(new ApprovePnp(revision.Id)));
            Assert.False((await new ApprovePnpHandler(contexts, user).HandleAsync(new ApprovePnp(revision.Id))).IsSuccess);
            Assert.False((await new ApproveBomHandler(contexts, user).HandleAsync(new ApproveBom(revision.Id, BomVersion.ForOrder, "118/26", null))).IsSuccess);
            Ok(await new ApproveBomHandler(contexts, user).HandleAsync(new ApproveBom(revision.Id, BomVersion.ForOrder, "118/2026", "pilot")));

            var version = Assert.Single(await reader.VersionsAsync(packageId));
            Assert.Equal((1, "118/2026", 2, 1), (version.Number, version.OrderNumber, version.LineCount, version.UnconfirmedCount));
            using var snapshot = JsonDocument.Parse(version.LinesJson);
            var j4Copy = snapshot.RootElement.EnumerateArray().Single(e => e.GetProperty("LineNumber").GetInt32() == 2);
            Assert.Equal(("CONN 6P THT", "THT"), (j4Copy.GetProperty("PartTs").GetString(), j4Copy.GetProperty("MountingCode").GetString()));

            // Każdy krok ma wpis w historii pakietu, z autorem — kontem wybranym przy starcie.
            var history = await new SqlPackageHistoryReader(contexts).ListAsync(packageId);
            Assert.True(history.Count >= 10);
            Assert.All(history.Where(h => h.Operation != "utworzenie" && h.Operation != "import BOM"), h => Assert.Equal("Opiekun (test)", h.Author));
        }
        finally
        {
            await using var context = contexts.CreateDbContext();
            await context.Database.EnsureDeletedAsync();
        }
    }

    private static void Ok(ProduMan.SharedKernel.Common.Result result) => Assert.True(result.IsSuccess, result.Error);

    private static IPartContextFactory TemporaryContexts()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "iPart.slnx"))) directory = directory.Parent;
        using var configured = new IPartDbContextFactory().CreateDbContext(["--settings", TestSettings.Path(directory!.FullName)]);
        var builder = new SqlConnectionStringBuilder(configured.Database.GetConnectionString())
        {
            InitialCatalog = "iPart_Verify_" + Guid.NewGuid().ToString("N")[..12], Pooling = false
        };
        return new IPartContextFactory(new DbContextOptionsBuilder<IPartDbContext>().UseSqlServer(builder.ConnectionString).Options);
    }
}
