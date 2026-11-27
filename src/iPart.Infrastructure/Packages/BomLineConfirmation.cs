using System.Globalization;
using iPart.Core.Packages;
using iPart.Core.Shared;
using Microsoft.EntityFrameworkCore;
using ProduMan.SharedKernel.Common;

namespace iPart.Infrastructure.Packages;

/// <summary>
/// Potwierdzenie linii BOM z podstawą (1.5). Jedna operacja = jedna transakcja = jeden zestaw zmian w historii (ADR-0007).
/// Błąd biznesowy to <see cref="Result.Fail"/>, nie wyjątek.
/// </summary>
public sealed class ConfirmBomLineHandler(IDbContextFactory<IPartDbContext> contexts, ICurrentUser currentUser) : IConfirmBomLineHandler
{
    public const int MaxNoteLength = 1000;

    public async Task<Result> HandleAsync(ConfirmBomLine command, CancellationToken cancellationToken = default)
    {
        if (!ConfirmationBases.Names.ContainsKey(command.Basis)) return Result.Fail("Wybierz podstawę potwierdzenia.");
        var note = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim();
        if (command.Basis == ConfirmationBases.CoordinatorDecision && note is null)
            return Result.Fail("Decyzja Opiekuna wymaga uzasadnienia.");
        if (note?.Length > MaxNoteLength) return Result.Fail($"Uzasadnienie może mieć najwyżej {MaxNoteLength} znaków.");

        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        var line = await context.BomLines.SingleOrDefaultAsync(x => x.Id == command.LineId, cancellationToken);
        if (line is null) return Result.Fail("Linia nie istnieje (mogła zostać usunięta).");
        if (line.Confirmed) return Result.Fail($"Linia {line.LineNumber} jest już potwierdzona.");
        var packageId = await context.BomRevisions.Where(r => r.Id == line.BomRevisionId).Select(r => r.PackageId).SingleAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        line.Confirmed = true;
        line.ConfirmationBasis = command.Basis;
        line.ConfirmationNote = note;
        line.ConfirmedByUserId = currentUser.UserId;
        line.ConfirmedBy = currentUser.GetAuthor();
        line.ConfirmedAtUtc = now;

        var prefix = $"Linia {line.LineNumber.ToString(CultureInfo.InvariantCulture)}.";
        context.DocumentationChangeSets.Add(new DocumentationChangeSet
        {
            UserId = currentUser.UserId, Author = currentUser.GetAuthor(), OccurredAtUtc = now,
            SubjectType = nameof(Package), SubjectId = packageId, Operation = ChangeOperations.Confirmed, Comment = note,
            Changes =
            [
                new() { ObjectType = nameof(BomLine), ObjectId = line.Id, Field = prefix + nameof(BomLine.Confirmed), PreviousValue = "false", NewValue = "true" },
                new() { ObjectType = nameof(BomLine), ObjectId = line.Id, Field = prefix + nameof(BomLine.ConfirmationBasis), NewValue = command.Basis },
            ]
        });
        await context.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}

/// <summary>Historia pakietu do wyświetlenia — serwis odczytu, bez ładowania agregatów do edycji.</summary>
public sealed class SqlPackageHistoryReader(IDbContextFactory<IPartDbContext> contexts) : IPackageHistoryReader
{
    public async Task<IReadOnlyList<HistoryEntry>> ListAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        var sets = await context.DocumentationChangeSets.AsNoTracking().Include(x => x.Changes)
            .Where(x => x.SubjectId == packageId).OrderByDescending(x => x.OccurredAtUtc).Take(200).ToListAsync(cancellationToken);
        return sets.Select(s => new HistoryEntry(s.OccurredAtUtc, s.Author, Describe(s.Operation), s.Comment,
            s.Changes.OrderBy(c => c.Id).Select(c => c.PreviousValue is null ? $"{c.Field}: {Show(c)}" : $"{c.Field}: {c.PreviousValue} → {Show(c)}").ToList())).ToList();
    }

    private static string Show(FieldChange change) =>
        change.Field.EndsWith(nameof(BomLine.ConfirmationBasis), StringComparison.Ordinal) ? ConfirmationBases.Describe(change.NewValue) : change.NewValue ?? "—";

    private static string Describe(string operation) => operation switch
    {
        ChangeOperations.Created => "utworzenie",
        ChangeOperations.Imported => "import BOM",
        ChangeOperations.Confirmed => "potwierdzenie linii",
        ChangeOperations.Updated => "zmiana",
        _ => operation
    };
}

/// <summary>Kopia listy kont i słownika działów ProduMana (ADR-0014) — tylko odczyt.</summary>
public sealed class SqlAccountDirectory(IDbContextFactory<IPartDbContext> contexts) : IAccountDirectory
{
    public async Task<IReadOnlyList<ProduManAccount>> ListSelectableAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        return await context.ProduManAccounts.AsNoTracking().Where(x => x.IsActive && !x.Deleted)
            .OrderBy(x => x.FullName).ThenBy(x => x.Login).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, Department>> DepartmentsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        return await context.Departments.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
    }
}
