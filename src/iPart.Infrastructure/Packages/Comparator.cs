using iPart.Core.Packages;
using iPart.Core.Shared;
using Microsoft.EntityFrameworkCore;
using ProduMan.SharedKernel.Common;

namespace iPart.Infrastructure.Packages;

/// <summary>
/// Comparator po każdej zmianie (P&amp;P, wybór pól, rozdzielenie linii):
/// pusty Footprint w BOM i jeden Footprint w P&amp;P → Footprint przyjęty z P&amp;P;
/// linia bez żadnej rozbieżności ani ostrzeżenia zatwierdza się sama (podstawa: zgodność z P&amp;P);
/// linia zatwierdzona tak wcześniej, która teraz ma różnice, wraca do sprawdzenia.
/// </summary>
internal static class Comparator
{
    public static void Sync(BomRevision revision, IReadOnlyList<BomLine> lines, IReadOnlyList<PnpRow> pnp, ICurrentUser user, List<DocumentationFieldChange> changes)
    {
        var options = ComparatorOptions.Parse(revision.CompareFields);
        var comparison = BomPnpComparer.Compare(lines, pnp, options);
        var now = DateTimeOffset.UtcNow;
        foreach (var line in lines)
        {
            var footprint = LineFields.For(line, pnp, options).Single(f => f.Field == ComparedFields.Footprint);
            if (footprint.TakenFromPnp && line.TsFootprint is null && !line.Confirmed)
            {
                changes.Add(Audit.Field(nameof(BomLine), line.Id, Audit.Line(line, "Footprint"), null, $"{footprint.PnpSingle} (z P&P — brak w BOM)"));
                line.TsFootprint = footprint.PnpSingle;
            }
            var clean = pnp.Count > 0 && comparison.Lines.Single(c => c.LineId == line.Id).Differences.Count == 0;
            if (clean && !line.Confirmed && line.ComparisonDecision is null)
            {
                line.Confirmed = true;
                line.ConfirmationBasis = ConfirmationBases.PnpMatch;
                line.ConfirmationNote = null;
                line.ConfirmedByUserId = user.UserId;
                line.ConfirmedBy = user.GetAuthor();
                line.ConfirmedAtUtc = now;
                changes.Add(Audit.Field(nameof(BomLine), line.Id, Audit.Line(line, "Confirmed"), "false", "true (zgodność z P&P)"));
            }
            else if (!clean && line.Confirmed && line.ConfirmationBasis == ConfirmationBases.PnpMatch)
            {
                line.Confirmed = false;
                line.ConfirmationBasis = null;
                line.ConfirmedByUserId = null;
                line.ConfirmedBy = null;
                line.ConfirmedAtUtc = null;
                changes.Add(Audit.Field(nameof(BomLine), line.Id, Audit.Line(line, "Confirmed"), "true (zgodność z P&P)", "false — są różnice z P&P"));
            }
        }
    }
}

/// <summary>Comparator: wybór porównywanych pól (Value, Footprint, Description), zapisany przy rewizji importu.</summary>
public sealed class SetComparatorOptionsHandler(IDbContextFactory<IPartDbContext> contexts, ICurrentUser user) : ICommandHandler<SetComparatorOptions>
{
    public async Task<Result> HandleAsync(SetComparatorOptions command, CancellationToken ct = default)
    {
        await using var context = await contexts.CreateDbContextAsync(ct);
        var revision = await context.BomRevisions.SingleOrDefaultAsync(x => x.Id == command.RevisionId, ct);
        if (revision is null) return Result.Fail("Rewizja importu nie istnieje.");
        var before = ComparatorOptions.Parse(revision.CompareFields);
        if (before == command.Options) return Result.Ok();
        revision.CompareFields = command.Options.Store();
        var changes = new List<DocumentationFieldChange>
        {
            Audit.Field(nameof(BomRevision), revision.Id, $"Rewizja {revision.Number}.Porównywane pola", before.Describe(), command.Options.Describe())
        };
        var lines = await context.BomLines.Include(x => x.ExpandedDesignators).Where(x => x.BomRevisionId == revision.Id).ToListAsync(ct);
        var pnp = await context.PnpRows.AsNoTracking().Where(x => x.BomRevisionId == revision.Id).ToListAsync(ct);
        Comparator.Sync(revision, lines, pnp, user, changes);
        context.DocumentationChangeSets.Add(Audit.Set(user, revision.PackageId, ChangeOperations.Updated, null, changes.ToArray()));
        await context.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

/// <summary>
/// Rozdziela linię według grup P&amp;P (np. R1–R10 w BOM, w P&amp;P R1–R5 0402 i R6–R10 0603): największa grupa zostaje w linii,
/// pozostałe stają się nowymi liniami na końcu (dane Klienta, kolumny dodatkowe i MPN skopiowane; odnośnik do linii źródłowej).
/// </summary>
public sealed class SplitLineHandler(IDbContextFactory<IPartDbContext> contexts, ICurrentUser user) : ICommandHandler<SplitLine>
{
    public async Task<Result> HandleAsync(SplitLine command, CancellationToken ct = default)
    {
        await using var context = await contexts.CreateDbContextAsync(ct);
        var line = await context.BomLines.Include(x => x.ExpandedDesignators).Include(x => x.ExtraValues).SingleOrDefaultAsync(x => x.Id == command.LineId, ct);
        if (line is null) return Result.Fail("Linia nie istnieje.");
        if (line.Confirmed) return Result.Fail($"Linia {line.LineNumber} jest już zatwierdzona — nie można jej rozdzielić.");
        var revision = await context.BomRevisions.SingleAsync(r => r.Id == line.BomRevisionId, ct);
        var pnp = await context.PnpRows.AsNoTracking().Where(x => x.BomRevisionId == revision.Id).ToListAsync(ct);
        var groups = LineFields.SplitGroups(line, pnp, ComparatorOptions.Parse(revision.CompareFields));
        if (groups.Count < 2) return Result.Fail($"Linia {line.LineNumber}: wszystkie desygnatory mają w P&P te same dane — nie ma czego rozdzielać.");

        var mpns = await context.LineMpns.AsNoTracking().Where(m => m.BomLineId == line.Id).ToListAsync(ct);
        var next = await context.BomLines.Where(x => x.BomRevisionId == revision.Id).MaxAsync(x => x.LineNumber, ct);
        var changes = new List<DocumentationFieldChange>();
        var originalDesignators = line.Designators;
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        // Krok 1: desygnatory pozostałych grup znikają z linii źródłowej (desygnator jest unikalny w rewizji).
        var keep = groups[0].ToHashSet(StringComparer.Ordinal);
        context.BomLineDesignators.RemoveRange(line.ExpandedDesignators.Where(d => !keep.Contains(d.Designator)).ToList());
        line.Designators = DesignatorRanges.Compress(groups[0]);
        if (line.Quantity is not null) line.Quantity = groups[0].Count;
        changes.Add(Audit.Field(nameof(BomLine), line.Id, Audit.Line(line, "Designators"), originalDesignators, line.Designators));
        await context.SaveChangesAsync(ct);

        // Krok 2: nowe linie dla pozostałych grup.
        foreach (var group in groups.Skip(1))
        {
            var copy = new BomLine
            {
                BomRevisionId = revision.Id, LineNumber = ++next, SplitFromLineNumber = line.LineNumber,
                Designators = DesignatorRanges.Compress(group), Quantity = group.Count,
                PartTs = line.PartTs, Footprint = line.Footprint, Description = line.Description, Value = line.Value,
                PartNumber = line.PartNumber, Notes = line.Notes, MountingCode = line.MountingCode,
            };
            copy.ExpandedDesignators = group.Select(d => new BomLineDesignator { BomLineId = copy.Id, BomRevisionId = revision.Id, Designator = d }).ToList();
            copy.ExtraValues = line.ExtraValues.Select(e => new BomLineExtraValue { BomLineId = copy.Id, Ordinal = e.Ordinal, Header = e.Header, Value = e.Value }).ToList();
            context.BomLines.Add(copy);
            foreach (var m in mpns)
                context.LineMpns.Add(CopyMpn(m, copy.Id));
            changes.Add(Audit.Field(nameof(BomLine), copy.Id, Audit.Line(copy, "Utworzona"), null, $"wydzielona z linii {line.LineNumber}: {copy.Designators}"));
        }
        await context.SaveChangesAsync(ct);

        var lines = await context.BomLines.Include(x => x.ExpandedDesignators).Where(x => x.BomRevisionId == revision.Id).ToListAsync(ct);
        Comparator.Sync(revision, lines, pnp, user, changes);
        context.DocumentationChangeSets.Add(Audit.Set(user, revision.PackageId, ChangeOperations.Updated, $"Rozdzielenie linii {line.LineNumber} według P&P", changes.ToArray()));
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result.Ok();
    }

    private static LineMpn CopyMpn(LineMpn source, Guid lineId)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        var copy = System.Text.Json.JsonSerializer.Deserialize<LineMpn>(json)!;
        copy.Id = Guid.NewGuid();
        copy.BomLineId = lineId;
        return copy;
    }
}

/// <summary>Powrót z kroku 2 do kroku 1: usuwa wczytany BOM (linie, P&amp;P, MPN linii), jeśli nie ma z niego zatwierdzonej wersji. Wpis w historii zostaje.</summary>
public sealed class DiscardBomRevisionHandler(IDbContextFactory<IPartDbContext> contexts, ICurrentUser user) : ICommandHandler<DiscardBomRevision>
{
    public async Task<Result> HandleAsync(DiscardBomRevision command, CancellationToken ct = default)
    {
        await using var context = await contexts.CreateDbContextAsync(ct);
        var revision = await context.BomRevisions.SingleOrDefaultAsync(x => x.Id == command.RevisionId, ct);
        if (revision is null) return Result.Ok();
        if (await context.BomVersions.AnyAsync(v => v.BomRevisionId == revision.Id, ct))
            return Result.Fail("Z tego BOM jest już zatwierdzona wersja — nie można go usunąć.");
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        var lineIds = context.BomLines.Where(l => l.BomRevisionId == revision.Id).Select(l => l.Id);
        await context.LineMpns.Where(m => lineIds.Contains(m.BomLineId)).ExecuteDeleteAsync(ct);
        await context.BomLineDesignators.Where(d => d.BomRevisionId == revision.Id).ExecuteDeleteAsync(ct);
        await context.BomLineExtraValues.Where(e => lineIds.Contains(e.BomLineId)).ExecuteDeleteAsync(ct);
        await context.PnpRows.Where(p => p.BomRevisionId == revision.Id).ExecuteDeleteAsync(ct);
        await context.BomLines.Where(l => l.BomRevisionId == revision.Id).ExecuteDeleteAsync(ct);
        context.BomRevisions.Remove(revision);
        context.DocumentationChangeSets.Add(Audit.Set(user, revision.PackageId, ChangeOperations.Updated, "Powrót do kroku 1 (Import BOM) — wczytany BOM usunięty",
            Audit.Field(nameof(BomRevision), revision.Id, $"Rewizja {revision.Number}", revision.SourceFileName, null)));
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result.Ok();
    }
}

/// <summary>
/// Powrót z kroku 3 do kroku 2: usuwa wiersze P&amp;P, scala linie rozdzielone z powrotem do linii źródłowych
/// i czyści rozstrzygnięcia (zatwierdzenia, Wykaz Różnic, wartości z Comparatora). Niemożliwe po zatwierdzeniu P&amp;P albo wersji BOM.
/// </summary>
public sealed class ResetComparatorHandler(IDbContextFactory<IPartDbContext> contexts, ICurrentUser user) : ICommandHandler<ResetComparator>
{
    public async Task<Result> HandleAsync(ResetComparator command, CancellationToken ct = default)
    {
        await using var context = await contexts.CreateDbContextAsync(ct);
        var revision = await context.BomRevisions.SingleOrDefaultAsync(x => x.Id == command.RevisionId, ct);
        if (revision is null) return Result.Fail("Wczytany BOM nie istnieje.");
        if (revision.PnpApprovedAtUtc is not null) return Result.Fail("P&P jest już zatwierdzone — nie można wrócić do importu P&P.");
        if (await context.BomVersions.AnyAsync(v => v.BomRevisionId == revision.Id, ct))
            return Result.Fail("Z tego BOM jest już zatwierdzona wersja — nie można wrócić do importu P&P.");

        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        var lines = await context.BomLines.Include(l => l.ExpandedDesignators).Where(l => l.BomRevisionId == revision.Id).ToListAsync(ct);

        // Linie rozdzielone wracają do linii źródłowych (najpierw usunięcie, potem dopisanie desygnatorów — desygnator jest unikalny w rewizji).
        var split = lines.Where(l => l.SplitFromLineNumber is not null).OrderByDescending(l => l.LineNumber).ToList();
        var moved = new List<(BomLine Target, string Designator, int? Quantity)>();
        foreach (var line in split)
        {
            var target = lines.First(l => l.LineNumber == line.SplitFromLineNumber);
            while (target.SplitFromLineNumber is { } from) target = lines.First(l => l.LineNumber == from);
            moved.AddRange(line.ExpandedDesignators.Select(d => (target, d.Designator, (int?)null)));
            if (target.Quantity is not null && line.Quantity is not null) target.Quantity += line.Quantity;
        }
        if (split.Count > 0)
        {
            var splitIds = split.Select(l => l.Id).ToList();
            await context.LineMpns.Where(m => splitIds.Contains(m.BomLineId)).ExecuteDeleteAsync(ct);
            context.BomLines.RemoveRange(split);
            await context.SaveChangesAsync(ct);
            foreach (var group in moved.GroupBy(m => m.Target))
            {
                // Lista przed dopisaniem — EF dopina nowe desygnatory do kolekcji linii, nie liczymy ich dwa razy.
                var all = group.Key.ExpandedDesignators.Select(d => d.Designator).Concat(group.Select(g => g.Designator))
                    .Distinct(StringComparer.Ordinal).ToList();
                foreach (var (target, designator, _) in group)
                    context.BomLineDesignators.Add(new BomLineDesignator { BomLineId = target.Id, BomRevisionId = revision.Id, Designator = designator });
                group.Key.Designators = DesignatorRanges.Compress(all);
            }
        }

        foreach (var line in lines.Except(split))
        {
            line.Confirmed = false;
            line.ConfirmationBasis = line.ConfirmationNote = line.ConfirmedBy = null;
            line.ConfirmedByUserId = null;
            line.ConfirmedAtUtc = null;
            line.ComparisonDecision = line.DifferenceQuestion = line.CustomerAnswer = null;
            line.TsFootprint = line.TsDescription = line.TsValue = line.TsPartNumber = null;
        }
        await context.PnpRows.Where(p => p.BomRevisionId == revision.Id).ExecuteDeleteAsync(ct);
        var pnpFile = revision.PnpFileName;
        revision.PnpFileId = null;
        revision.PnpFileName = null;
        context.DocumentationChangeSets.Add(Audit.Set(user, revision.PackageId, ChangeOperations.Updated, "Powrót do kroku 2 (Import P&P) — Comparator wyczyszczony",
            Audit.Field(nameof(BomRevision), revision.Id, $"Rewizja {revision.Number}.P&P", pnpFile, null)));
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result.Ok();
    }
}
