using System.Globalization;
using System.Text.Json;
using iPart.Core.Packages;
using iPart.Core.Shared;
using Microsoft.EntityFrameworkCore;
using ProduMan.SharedKernel.Common;

namespace iPart.Infrastructure.Packages;

// Handlery komend procesu BOM (comparator + iPart). Każda komenda: walidacja → zapis → jeden zestaw zmian w historii pakietu
// w tej samej transakcji (ADR-0007). Błąd biznesowy to Result.Fail, nie wyjątek.

internal static class Audit
{
    public static DocumentationChangeSet Set(ICurrentUser user, Guid packageId, string operation, string? comment, params DocumentationFieldChange[] changes) => new()
    {
        UserId = user.UserId, Author = user.GetAuthor(), OccurredAtUtc = DateTimeOffset.UtcNow,
        SubjectType = nameof(Package), SubjectId = packageId, Operation = operation, Comment = comment, Changes = changes.ToList()
    };

    public static DocumentationFieldChange Field(string objectType, Guid objectId, string field, string? before, string? after) =>
        new() { ObjectType = objectType, ObjectId = objectId, Field = field, PreviousValue = before, NewValue = after };

    public static string Line(BomLine line, string field) => $"Linia {line.LineNumber.ToString(CultureInfo.InvariantCulture)}.{field}";

    public static async Task<(BomLine? Line, Guid PackageId)> LineAsync(IPartDbContext context, Guid lineId, CancellationToken ct)
    {
        var line = await context.BomLines.SingleOrDefaultAsync(x => x.Id == lineId, ct);
        if (line is null) return (null, Guid.Empty);
        var packageId = await context.BomRevisions.Where(r => r.Id == line.BomRevisionId).Select(r => r.PackageId).SingleAsync(ct);
        return (line, packageId);
    }

    public static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}

/// <summary>Dołącza P&amp;P Klienta do rewizji importu: kopia pliku w bazie (ADR-0008) i wiersze w brzmieniu oryginalnym.</summary>
public sealed class AttachPnpHandler(IDbContextFactory<IPartDbContext> contexts, ICurrentUser user) : ICommandHandler<AttachPnp>
{
    public async Task<Result> HandleAsync(AttachPnp command, CancellationToken ct = default)
    {
        if (command.Content.LongLength == 0) return Result.Fail("Plik P&P jest pusty.");
        if (command.Content.LongLength > StoredFile.MaxSizeBytes) return Result.Fail("Plik P&P jest za duży (max 50 MB).");
        if (command.Rows.Count == 0) return Result.Fail("Brak wierszy P&P do zapisania.");
        await using var context = await contexts.CreateDbContextAsync(ct);
        var revision = await context.BomRevisions.SingleOrDefaultAsync(x => x.Id == command.RevisionId, ct);
        if (revision is null) return Result.Fail("Rewizja importu nie istnieje.");
        if (revision.PnpApprovedAtUtc is not null) return Result.Fail("P&P tej rewizji jest już zatwierdzone — nowy P&P wymaga nowej rewizji importu.");

        var now = DateTimeOffset.UtcNow;
        var file = new DocumentationFile
        {
            FileName = Path.GetFileName(command.FileName), ContentType = Path.GetExtension(command.FileName).Equals(".txt", StringComparison.OrdinalIgnoreCase) ? "text/plain" : null,
            SizeBytes = command.Content.LongLength, Sha256 = StoredFile.ComputeSha256(command.Content), Content = command.Content,
            UploadedByUserId = user.UserId, UploadedBy = user.GetAuthor(), UploadedAtUtc = now
        };
        context.DocumentationFiles.Add(file);
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await context.PnpRows.Where(x => x.BomRevisionId == revision.Id).ExecuteDeleteAsync(ct);
        foreach (var row in command.Rows) { row.Id = Guid.NewGuid(); row.BomRevisionId = revision.Id; context.PnpRows.Add(row); }
        var before = revision.PnpFileName;
        revision.PnpFileId = file.Id;
        revision.PnpFileName = file.FileName;
        if (command.Options is { } options) revision.CompareFields = options.Store();
        var changes = new List<DocumentationFieldChange>
        {
            Audit.Field(nameof(BomRevision), revision.Id, $"Rewizja {revision.Number}.P&P", before, $"{file.FileName} ({command.Rows.Count} wierszy)")
        };

        var lines = await context.BomLines.Include(x => x.ExpandedDesignators).Where(x => x.BomRevisionId == revision.Id).ToListAsync(ct);
        Comparator.Sync(revision, lines, command.Rows, user, changes);
        context.DocumentationChangeSets.Add(Audit.Set(user, revision.PackageId, ChangeOperations.Imported, null, changes.ToArray()));
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result.Ok();
    }
}

/// <summary>Decyzja po porównaniu z P&amp;P: „sprawdziłem” albo pytanie do Wykazu Różnic (wtedy pytanie jest obowiązkowe).</summary>
public sealed class SetComparisonDecisionHandler(IDbContextFactory<IPartDbContext> contexts, ICurrentUser user) : ICommandHandler<SetComparisonDecision>
{
    public async Task<Result> HandleAsync(SetComparisonDecision command, CancellationToken ct = default)
    {
        if (command.Decision is not (null or ComparisonDecisions.Checked or ComparisonDecisions.DifferenceList)) return Result.Fail("Nieznana decyzja.");
        var question = Audit.Clean(command.Question);
        if (command.Decision == ComparisonDecisions.DifferenceList && question is null) return Result.Fail("Wpisz pytanie do Klienta dla Wykazu Różnic.");
        await using var context = await contexts.CreateDbContextAsync(ct);
        var (line, packageId) = await Audit.LineAsync(context, command.LineId, ct);
        if (line is null) return Result.Fail("Linia nie istnieje.");
        if (line.Confirmed) return Result.Fail($"Linia {line.LineNumber} jest już potwierdzona.");
        var change = Audit.Set(user, packageId, ChangeOperations.Updated, question,
            Audit.Field(nameof(BomLine), line.Id, Audit.Line(line, "Porównanie"), line.ComparisonDecision, command.Decision));
        line.ComparisonDecision = command.Decision;
        line.DifferenceQuestion = command.Decision == ComparisonDecisions.DifferenceList ? question : null;
        context.DocumentationChangeSets.Add(change);
        await context.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

/// <summary>Odpowiedź Klienta na pytanie z Wykazu Różnic — podstawa do potwierdzenia linii.</summary>
public sealed class RecordCustomerAnswerHandler(IDbContextFactory<IPartDbContext> contexts, ICurrentUser user) : ICommandHandler<RecordCustomerAnswer>
{
    public async Task<Result> HandleAsync(RecordCustomerAnswer command, CancellationToken ct = default)
    {
        var answer = Audit.Clean(command.Answer);
        if (answer is null) return Result.Fail("Wpisz odpowiedź Klienta.");
        await using var context = await contexts.CreateDbContextAsync(ct);
        var (line, packageId) = await Audit.LineAsync(context, command.LineId, ct);
        if (line is null) return Result.Fail("Linia nie istnieje.");
        if (line.ComparisonDecision != ComparisonDecisions.DifferenceList) return Result.Fail("Linia nie jest w Wykazie Różnic.");
        context.DocumentationChangeSets.Add(Audit.Set(user, packageId, ChangeOperations.Updated, null,
            Audit.Field(nameof(BomLine), line.Id, Audit.Line(line, "Odpowiedź Klienta"), line.CustomerAnswer, answer)));
        line.CustomerAnswer = answer;
        await context.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

/// <summary>Zmiana sekcji TS (Part TS, sposób montażu). Dane Klienta (Oryginał) zostają bez zmian.</summary>
public sealed class UpdateLineTsHandler(IDbContextFactory<IPartDbContext> contexts, ICurrentUser user) : ICommandHandler<UpdateLineTs>
{
    public async Task<Result> HandleAsync(UpdateLineTs command, CancellationToken ct = default)
    {
        var partTs = Audit.Clean(command.PartTs);
        var mounting = Audit.Clean(command.MountingCode);
        if (partTs?.Length > 500) return Result.Fail("Part TS może mieć najwyżej 500 znaków.");
        await using var context = await contexts.CreateDbContextAsync(ct);
        if (mounting is not null && !await context.MountingMethods.AnyAsync(x => x.Code == mounting, ct)) return Result.Fail("Nieznany sposób montażu.");
        var (line, packageId) = await Audit.LineAsync(context, command.LineId, ct);
        if (line is null) return Result.Fail("Linia nie istnieje.");
        var changes = new List<DocumentationFieldChange>();
        if (line.PartTs != partTs) changes.Add(Audit.Field(nameof(BomLine), line.Id, Audit.Line(line, "Part TS"), line.PartTs, partTs));
        if (line.MountingCode != mounting) changes.Add(Audit.Field(nameof(BomLine), line.Id, Audit.Line(line, "Montaż"), line.MountingCode, mounting));
        if (changes.Count == 0) return Result.Ok();
        line.PartTs = partTs;
        line.MountingCode = mounting;
        context.DocumentationChangeSets.Add(Audit.Set(user, packageId, ChangeOperations.Updated, Audit.Clean(command.Comment), changes.ToArray()));
        await context.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

/// <summary>Dodanie MPN zatwierdzonego (z podstawą) albo niedozwolonego (z powodem). Niedozwolonego nie da się jednocześnie zatwierdzić.</summary>
public sealed class AddLineMpnHandler(IDbContextFactory<IPartDbContext> contexts, ICurrentUser user) : ICommandHandler<AddLineMpn>
{
    public async Task<Result> HandleAsync(AddLineMpn command, CancellationToken ct = default)
    {
        var mpn = Audit.Clean(command.Mpn);
        var note = Audit.Clean(command.Note);
        if (mpn is null) return Result.Fail("Wpisz MPN.");
        if (note is null) return Result.Fail(command.Forbidden ? "Podaj powód zakazu." : "Podaj podstawę zatwierdzenia.");
        await using var context = await contexts.CreateDbContextAsync(ct);
        var (line, packageId) = await Audit.LineAsync(context, command.LineId, ct);
        if (line is null) return Result.Fail("Linia nie istnieje.");
        var existing = await context.LineMpns.Where(x => x.BomLineId == line.Id).ToListAsync(ct);
        if (existing.Any(x => x.Mpn == mpn && x.Forbidden == command.Forbidden)) return Result.Fail("Ten MPN jest już na liście.");
        if (!command.Forbidden && existing.Any(x => x.Mpn == mpn && x.Forbidden)) return Result.Fail("Ten MPN jest niedozwolony dla linii — nie można go zatwierdzić.");
        var entry = new LineMpn
        {
            BomLineId = line.Id, Mpn = mpn, Forbidden = command.Forbidden, Note = note, Ordinal = existing.Count + 1,
            AddedByUserId = user.UserId, AddedBy = user.GetAuthor(), AddedAtUtc = DateTimeOffset.UtcNow
        };
        context.LineMpns.Add(entry);
        context.DocumentationChangeSets.Add(Audit.Set(user, packageId, ChangeOperations.Updated, note,
            Audit.Field(nameof(LineMpn), entry.Id, Audit.Line(line, command.Forbidden ? "MPN niedozwolony" : "MPN zatwierdzony"), null, mpn)));
        await context.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

/// <summary>Usunięcie MPN z listy — tylko z powodem (2.11), ślad w historii.</summary>
public sealed class RemoveLineMpnHandler(IDbContextFactory<IPartDbContext> contexts, ICurrentUser user) : ICommandHandler<RemoveLineMpn>
{
    public async Task<Result> HandleAsync(RemoveLineMpn command, CancellationToken ct = default)
    {
        var reason = Audit.Clean(command.Reason);
        if (reason is null) return Result.Fail("Podaj powód usunięcia.");
        await using var context = await contexts.CreateDbContextAsync(ct);
        var entry = await context.LineMpns.SingleOrDefaultAsync(x => x.Id == command.MpnId, ct);
        if (entry is null) return Result.Fail("MPN nie istnieje na liście.");
        var (line, packageId) = await Audit.LineAsync(context, entry.BomLineId, ct);
        context.LineMpns.Remove(entry);
        context.DocumentationChangeSets.Add(Audit.Set(user, packageId, ChangeOperations.Updated, reason,
            Audit.Field(nameof(LineMpn), entry.Id, Audit.Line(line!, entry.Forbidden ? "MPN niedozwolony" : "MPN zatwierdzony"), entry.Mpn, null)));
        await context.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

/// <summary>Zatwierdzenie P&amp;P przez Technologa SMT (2.7). W pilotażu rola jest umowna — zapisujemy, kto zatwierdził.</summary>
public sealed class ApprovePnpHandler(IDbContextFactory<IPartDbContext> contexts, ICurrentUser user) : ICommandHandler<ApprovePnp>
{
    public async Task<Result> HandleAsync(ApprovePnp command, CancellationToken ct = default)
    {
        await using var context = await contexts.CreateDbContextAsync(ct);
        var revision = await context.BomRevisions.SingleOrDefaultAsync(x => x.Id == command.RevisionId, ct);
        if (revision is null) return Result.Fail("Rewizja importu nie istnieje.");
        if (revision.PnpFileId is null) return Result.Fail("Najpierw zaimportuj P&P do tej rewizji.");
        if (revision.PnpApprovedAtUtc is not null) return Result.Fail("P&P jest już zatwierdzone.");
        revision.PnpApprovedAtUtc = DateTimeOffset.UtcNow;
        revision.PnpApprovedBy = user.GetAuthor();
        revision.PnpApprovedByUserId = user.UserId;
        context.DocumentationChangeSets.Add(Audit.Set(user, revision.PackageId, ChangeOperations.Confirmed, null,
            Audit.Field(nameof(BomRevision), revision.Id, $"Rewizja {revision.Number}.P&P zatwierdzone", "false", "true")));
        await context.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

/// <summary>
/// Zatwierdzenie BOM (2.10): niezmienna wersja z pełną kopią linii — bazowa albo dla wskazanego ZK.
/// Linie niepotwierdzone wchodzą do wersji, ale zakupy ich nie liczą (4.3); P&amp;P niezatwierdzone nie blokuje (5.4).
/// </summary>
public sealed class ApproveBomHandler(IDbContextFactory<IPartDbContext> contexts, ICurrentUser user) : ICommandHandler<ApproveBom>
{
    public async Task<Result> HandleAsync(ApproveBom command, CancellationToken ct = default)
    {
        if (command.Kind is not (BomVersion.Base or BomVersion.ForOrder)) return Result.Fail("Wybierz rodzaj wersji.");
        string? order = null;
        if (command.Kind == BomVersion.ForOrder)
        {
            if (!OrderNumbers.IsValid(command.OrderNumber)) return Result.Fail("Podaj numer ZK w formacie nnn/rrrr.");
            order = OrderNumbers.Normalize(command.OrderNumber!);
        }
        await using var context = await contexts.CreateDbContextAsync(ct);
        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var revision = await context.BomRevisions.SingleOrDefaultAsync(x => x.Id == command.RevisionId, ct);
        if (revision is null) return Result.Fail("Rewizja importu nie istnieje.");
        var lines = await context.BomLines.AsNoTracking().Where(x => x.BomRevisionId == revision.Id).Include(x => x.ExpandedDesignators)
            .OrderBy(x => x.LineNumber).ToListAsync(ct);
        if (lines.Count == 0) return Result.Fail("Rewizja nie ma linii.");
        var lineIds = lines.Select(l => l.Id).ToList();
        var mpns = await context.LineMpns.AsNoTracking().Where(x => lineIds.Contains(x.BomLineId)).ToListAsync(ct);
        var number = (await context.BomVersions.Where(x => x.PackageId == revision.PackageId).MaxAsync(x => (int?)x.Number, ct) ?? 0) + 1;

        var snapshot = lines.Select(l => new
        {
            l.LineNumber, l.Designators, Count = l.ExpandedDesignators.Count, l.Quantity, l.PartTs, l.MountingCode,
            l.Description, l.Value, l.PartNumber, l.Footprint, l.Confirmed, l.ConfirmationBasis,
            Approved = mpns.Where(m => m.BomLineId == l.Id && !m.Forbidden).OrderBy(m => m.Ordinal).Select(m => new { m.Mpn, m.Note }),
            Forbidden = mpns.Where(m => m.BomLineId == l.Id && m.Forbidden).Select(m => new { m.Mpn, m.Note }),
        });
        var version = new BomVersion
        {
            PackageId = revision.PackageId, Number = number, BomRevisionId = revision.Id, Kind = command.Kind, OrderNumber = order,
            Comment = Audit.Clean(command.Comment), ApprovedByUserId = user.UserId, ApprovedBy = user.GetAuthor(), ApprovedAtUtc = DateTimeOffset.UtcNow,
            LinesJson = JsonSerializer.Serialize(snapshot), LineCount = lines.Count, UnconfirmedCount = lines.Count(l => !l.Confirmed)
        };
        context.BomVersions.Add(version);
        context.DocumentationChangeSets.Add(Audit.Set(user, revision.PackageId, ChangeOperations.Confirmed, version.Comment,
            Audit.Field(nameof(BomVersion), version.Id, "Wersja BOM", null, version.DisplayName)));
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result.Ok();
    }
}

/// <summary>Odczyty procesu BOM: P&amp;P, MPN linii, wersje, słownik sposobów montażu.</summary>
public sealed class SqlBomWorkflowReader(IDbContextFactory<IPartDbContext> contexts) : IBomWorkflowReader
{
    public async Task<BomRevision?> RevisionAsync(Guid revisionId, CancellationToken ct = default)
    {
        await using var context = await contexts.CreateDbContextAsync(ct);
        return await context.BomRevisions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == revisionId, ct);
    }

    public async Task<IReadOnlyList<PnpRow>> PnpRowsAsync(Guid revisionId, CancellationToken ct = default)
    {
        await using var context = await contexts.CreateDbContextAsync(ct);
        return await context.PnpRows.AsNoTracking().Where(x => x.BomRevisionId == revisionId).OrderBy(x => x.SourceRow).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LineMpn>> MpnsAsync(Guid revisionId, CancellationToken ct = default)
    {
        await using var context = await contexts.CreateDbContextAsync(ct);
        return await (from m in context.LineMpns.AsNoTracking()
                      join l in context.BomLines on m.BomLineId equals l.Id
                      where l.BomRevisionId == revisionId
                      orderby m.Forbidden, m.Ordinal
                      select m).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<BomVersion>> VersionsAsync(Guid packageId, CancellationToken ct = default)
    {
        await using var context = await contexts.CreateDbContextAsync(ct);
        return await context.BomVersions.AsNoTracking().Where(x => x.PackageId == packageId).OrderByDescending(x => x.Number).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MountingOption>> MountingMethodsAsync(CancellationToken ct = default)
    {
        await using var context = await contexts.CreateDbContextAsync(ct);
        return await context.MountingMethods.AsNoTracking().OrderBy(x => x.Id).Select(x => new MountingOption(x.Code, x.Name)).ToListAsync(ct);
    }
}

/// <summary>
/// Rozstrzygnięcie linii po porównaniu z P&amp;P i potwierdzenie (1.5): podstawa, odpowiedź Klienta albo komentarz,
/// wartości do sekcji TS dla różniących się pól (z BOM, z P&amp;P albo własne). Oryginał Klienta zostaje bez zmian.
/// </summary>
public sealed class ResolveLineHandler(IDbContextFactory<IPartDbContext> contexts, ICurrentUser user) : ICommandHandler<ResolveLine>
{
    public async Task<Result> HandleAsync(ResolveLine command, CancellationToken ct = default)
    {
        if (command.Basis is not (ConfirmationBases.CoordinatorDecision or ConfirmationBases.CustomerAnswer or ConfirmationBases.PreviousRealization))
            return Result.Fail("Wybierz podstawę zatwierdzenia.");
        var note = Audit.Clean(command.Note);
        if (command.Basis == ConfirmationBases.CustomerAnswer && note is null) return Result.Fail("Wpisz odpowiedź Klienta.");
        if (command.Basis == ConfirmationBases.PreviousRealization && note is null) return Result.Fail("Wpisz komentarz — której realizacji dotyczy.");

        await using var context = await contexts.CreateDbContextAsync(ct);
        var line = await context.BomLines.Include(x => x.ExpandedDesignators).SingleOrDefaultAsync(x => x.Id == command.LineId, ct);
        if (line is null) return Result.Fail("Linia nie istnieje.");
        if (line.Confirmed) return Result.Fail($"Linia {line.LineNumber} jest już zatwierdzona.");
        var revision = await context.BomRevisions.AsNoTracking().SingleAsync(r => r.Id == line.BomRevisionId, ct);
        var pnp = await context.PnpRows.AsNoTracking().Where(x => x.BomRevisionId == revision.Id).ToListAsync(ct);

        var changes = new List<DocumentationFieldChange>();
        foreach (var values in LineFields.For(line, pnp, ComparatorOptions.Parse(revision.CompareFields)).Where(v => v.Differs))
        {
            var resolution = command.Resolutions.FirstOrDefault(r => r.Field == values.Field);
            if (resolution is null) return Result.Fail($"Wybierz wartość dla pola {values.Label} (BOM, P&P albo własna).");
            var value = LineFields.Value(values, resolution);
            if (value is null)
                return Result.Fail(resolution.Source == ValueSource.Pnp
                    ? $"{values.Label}: w P&P są różne wartości ({values.PnpText}) — wybierz BOM albo wpisz własną."
                    : $"{values.Label}: brak wartości do zapisania.");
            switch (values.Field)
            {
                case ComparedFields.Value:
                    changes.Add(Audit.Field(nameof(BomLine), line.Id, Audit.Line(line, "Value"), line.TsValue, value)); line.TsValue = value; break;
                case ComparedFields.Footprint:
                    changes.Add(Audit.Field(nameof(BomLine), line.Id, Audit.Line(line, "Footprint TS"), line.TsFootprint, value)); line.TsFootprint = value; break;
                case ComparedFields.PartNumber:
                    changes.Add(Audit.Field(nameof(BomLine), line.Id, Audit.Line(line, "PartNumber"), line.TsPartNumber, value)); line.TsPartNumber = value; break;
                case ComparedFields.Description:
                    changes.Add(Audit.Field(nameof(BomLine), line.Id, Audit.Line(line, "Description TS"), line.TsDescription, value)); line.TsDescription = value; break;
            }
        }

        if (command.Basis == ConfirmationBases.CustomerAnswer)
        {
            changes.Add(Audit.Field(nameof(BomLine), line.Id, Audit.Line(line, "Odpowiedź Klienta"), line.CustomerAnswer, note));
            line.CustomerAnswer = note;
        }
        if (line.ComparisonDecision is null) line.ComparisonDecision = ComparisonDecisions.Checked;
        line.Confirmed = true;
        line.ConfirmationBasis = command.Basis;
        line.ConfirmationNote = note;
        line.ConfirmedByUserId = user.UserId;
        line.ConfirmedBy = user.GetAuthor();
        line.ConfirmedAtUtc = DateTimeOffset.UtcNow;
        changes.Add(Audit.Field(nameof(BomLine), line.Id, Audit.Line(line, "Confirmed"), "false", ConfirmationBases.Describe(command.Basis)));
        context.DocumentationChangeSets.Add(Audit.Set(user, revision.PackageId, ChangeOperations.Confirmed, note, changes.ToArray()));
        await context.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
