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
                new BomSourceRow(3, ["J4", "1", "CONN-6", null]),
                new BomSourceRow(4, ["C1", "1", "CAP-1", "0603"])]);
            var import = BomImportBuilder.Build(table, [new(0, BomField.Designator), new(1, BomField.Quantity), new(2, BomField.PartNumber), new(3, BomField.Footprint)]);
            var revision = await service.SaveBomRevisionAsync(packageId, import, "bom.xlsx", "BOM", null, [1, 2, 3]);
            var lines = await service.GetLinesAsync(revision.Id);
            var mpns = await reader.MpnsAsync(revision.Id);
            Assert.Equal(4, mpns.Count);
            Assert.Equal(MpnBases.FromCustomerBomPreferred, mpns.Single(m => m.Mpn == "RC0402").Note);

            // P&P bez J4 → rozbieżność „Brak w P&P”.
            var pnpRows = new List<PnpRow> { new() { Designator = "R1", Footprint = "0402" }, new() { Designator = "R2", Footprint = "0402" }, new() { Designator = "C1", Footprint = "0805" } };
            Ok(await new AttachPnpHandler(contexts, user).HandleAsync(new AttachPnp(revision.Id, "pnp.txt", [9, 9], pnpRows)));
            // Linia bez rozbieżności (R1–R2) potwierdza się sama przy imporcie P&P.
            var autoConfirmed = (await service.GetLinesAsync(revision.Id)).Single(l => l.LineNumber == 1);
            Assert.Equal((true, ConfirmationBases.PnpMatch), (autoConfirmed.Confirmed, autoConfirmed.ConfirmationBasis));
            var comparison = BomPnpComparer.Compare(lines, await reader.PnpRowsAsync(revision.Id));
            var j4 = lines.Single(l => l.LineNumber == 2);
            Assert.Contains(comparison.Lines.Single(l => l.LineId == j4.Id).Differences, d => d.Kind == "Brak w P&P");

            // Wykaz Różnic → odpowiedź Klienta → potwierdzenie z podstawą.
            var decide = new SetComparisonDecisionHandler(contexts, user);
            Assert.False((await decide.HandleAsync(new SetComparisonDecision(j4.Id, ComparisonDecisions.DifferenceList, " "))).IsSuccess);
            Ok(await decide.HandleAsync(new SetComparisonDecision(j4.Id, ComparisonDecisions.DifferenceList, "Czy J4 jest montowane (THT)?")));
            Ok(await new RecordCustomerAnswerHandler(contexts, user).HandleAsync(new RecordCustomerAnswer(j4.Id, "Tak, THT ręcznie.")));
            var resolve = new ResolveLineHandler(contexts, user);
            Assert.False((await resolve.HandleAsync(new ResolveLine(j4.Id, ConfirmationBases.CustomerAnswer, " ", []))).IsSuccess);
            Ok(await resolve.HandleAsync(new ResolveLine(j4.Id, ConfirmationBases.CustomerAnswer, "Tak, THT ręcznie.", [])));

            // Zatwierdź z wyborem wartości: Footprint C1 różni się od P&P → bez wyboru błąd, z wyborem P&P zapis do sekcji TS.
            var c1 = lines.Single(l => l.LineNumber == 3);
            Assert.False((await resolve.HandleAsync(new ResolveLine(c1.Id, ConfirmationBases.CoordinatorDecision, null, []))).IsSuccess);
            Ok(await resolve.HandleAsync(new ResolveLine(c1.Id, ConfirmationBases.CoordinatorDecision, null, [new(ComparedFields.Footprint, ValueSource.Pnp, null)])));
            Assert.Equal("0805", (await service.GetLinesAsync(revision.Id)).Single(l => l.Id == c1.Id).TsFootprint);

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
            Assert.Equal((1, "118/2026", 3, 0), (version.Number, version.OrderNumber, version.LineCount, version.UnconfirmedCount));
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

    [SqlServerFact]
    public async Task ComparatorTakesFootprintFromPnpSplitsLineAndRespectsChosenFields()
    {
        var contexts = TemporaryContexts();
        try
        {
            await using (var context = contexts.CreateDbContext()) await context.Database.MigrateAsync();
            var user = new Session();
            user.SignIn(new ProduManAccount { UserId = Guid.NewGuid(), FullName = "Opiekun (test)", IsActive = true });
            var service = new SqlServerPackageService(contexts, user);
            var reader = new SqlBomWorkflowReader(contexts);
            var packageId = await service.CreatePackageAsync(new NewPackageRequest("PCBA-CMP", "", null, null));
            var table = new BomSourceTable(["Des", "Qty", "Val", "PN", "Desc"], [
                new BomSourceRow(2, ["R1-R10", "10", "100R", "RC-100R", null]),
                new BomSourceRow(3, ["C1", "1", "100n", "CAP-1", "CAP 100n"])]);
            var import = BomImportBuilder.Build(table, [new(0, BomField.Designator), new(1, BomField.Quantity), new(2, BomField.Value), new(3, BomField.PartNumber), new(4, BomField.Description)]);
            var revision = await service.SaveBomRevisionAsync(packageId, import, "bom.xlsx", "BOM", null, [1]);

            // P&P: R1–R5 0402, R6–R10 0603 (BOM bez Footprint); C1 z innym opisem.
            var pnp = Enumerable.Range(1, 10).Select(i => new PnpRow { Designator = $"R{i}", Value = "100R", Footprint = i <= 5 ? "0402" : "0603" })
                .Append(new PnpRow { Designator = "C1", Value = "100n", Footprint = "0402", Description = "Capacitor" }).ToList();
            Ok(await new AttachPnpHandler(contexts, user).HandleAsync(new AttachPnp(revision.Id, "pnp.txt", [9], pnp)));
            var lines = await service.GetLinesAsync(revision.Id);
            var c1 = lines.Single(l => l.LineNumber == 2);
            Assert.Equal("0402", c1.TsFootprint);   // pusty Footprint w BOM, jeden w P&P → z P&P
            Assert.False(c1.Confirmed);              // Description różni się

            // Opiekun odznacza Description → C1 zgodne, zatwierdza się samo.
            Ok(await new SetComparatorOptionsHandler(contexts, user).HandleAsync(new SetComparatorOptions(revision.Id, new ComparatorOptions(true, true, false))));
            Assert.Equal((true, ConfirmationBases.PnpMatch), (await service.GetLinesAsync(revision.Id)).Single(l => l.Id == c1.Id) is var c ? (c.Confirmed, c.ConfirmationBasis) : default);
            Assert.Equal("Value,Footprint", (await reader.RevisionAsync(revision.Id))!.CompareFields);

            // R1–R10: rozdzielenie według P&P → dwie linie po 5 szt., obie z Footprint z P&P i zatwierdzone automatycznie.
            var r = lines.Single(l => l.LineNumber == 1);
            Ok(await new SplitLineHandler(contexts, user).HandleAsync(new SplitLine(r.Id)));
            var after = await service.GetLinesAsync(revision.Id);
            Assert.Equal(3, after.Count);
            var split = after.Single(l => l.LineNumber == 3);
            Assert.Equal((1, 5, 5), (split.SplitFromLineNumber, split.Quantity, after.Single(l => l.Id == r.Id).Quantity));
            Assert.All(after, l => Assert.True(l.Confirmed));
            Assert.Equal(["0402", "0603"], after.Where(l => l.Value == "100R").Select(l => l.TsFootprint).Order());
            Assert.Equal(2, (await reader.MpnsAsync(revision.Id)).Count(m => m.Mpn == "RC-100R"));
            Assert.False((await new SplitLineHandler(contexts, user).HandleAsync(new SplitLine(r.Id))).IsSuccess);

            // Powrót do kroku 2: P&P i rozstrzygnięcia znikają, rozdzielona linia wraca do źródłowej.
            Ok(await new ResetComparatorHandler(contexts, user).HandleAsync(new ResetComparator(revision.Id)));
            var reset = await service.GetLinesAsync(revision.Id);
            Assert.Equal(2, reset.Count);
            var merged = reset.Single(l => l.Id == r.Id);
            Assert.Equal(("R1–R10", 10, 10), (merged.Designators, merged.Quantity, merged.ExpandedDesignators.Count));
            Assert.All(reset, l => Assert.False(l.Confirmed));
            Assert.All(reset, l => Assert.Null(l.TsFootprint));
            Assert.Empty(await reader.PnpRowsAsync(revision.Id));
            Assert.Single(await reader.MpnsAsync(revision.Id), m => m.Mpn == "RC-100R");

            // Powrót do kroku 1: wczytany BOM znika.
            Ok(await new DiscardBomRevisionHandler(contexts, user).HandleAsync(new DiscardBomRevision(revision.Id)));
            Assert.Empty(await service.ListRevisionsAsync(packageId));
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
