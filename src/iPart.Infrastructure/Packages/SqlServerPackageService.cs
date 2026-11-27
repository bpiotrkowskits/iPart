using System.Data;
using iPart.Core.Components;
using iPart.Core.Packages;
using iPart.Core.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace iPart.Infrastructure.Packages;

public sealed class SqlServerPackageService(IDbContextFactory<IPartDbContext> contexts, ICurrentUser currentUser) : IPackageService
{
    public async Task<IReadOnlyList<PackageSummary>> ListPackagesAsync(string? search, CancellationToken cancellationToken = default)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        var query = from p in context.Packages.AsNoTracking()
                    join c in context.Customers.AsNoTracking() on p.CustomerId equals c.Id into customers
                    from c in customers.DefaultIfEmpty()
                    select new { p, CustomerName = c == null ? null : c.Name };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            query = query.Where(x => x.p.Code.Contains(text) || (x.CustomerName != null && x.CustomerName.Contains(text)));
        }
        var rows = await query.OrderBy(x => x.p.Code).ThenBy(x => x.p.CustomerRevision)
            .Select(x => new PackageSummary(x.p.Id, x.p.Code, x.p.CustomerRevision, x.CustomerName, x.p.Description,
                context.BomRevisions.Count(r => r.PackageId == x.p.Id), x.p.CreatedAtUtc, x.p.CreatedBy))
            .ToListAsync(cancellationToken);
        return rows;
    }

    public async Task<Guid> CreatePackageAsync(NewPackageRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code?.Trim() ?? "";
        var revision = request.CustomerRevision?.Trim() ?? "";
        if (code.Length == 0) throw new PackageOperationException("Podaj kod pakietu.");
        if (revision.Length == 0) throw new PackageOperationException("Podaj rewizję Klienta (np. A, B, 1.0).");
        if (code.Length > 100 || revision.Length > 50) throw new PackageOperationException("Kod pakietu (max 100) lub rewizja (max 50) są za długie.");

        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        if (await context.Packages.AnyAsync(x => x.Code == code && x.CustomerRevision == revision && x.CustomerId == request.CustomerId, cancellationToken))
            throw new PackageOperationException($"Pakiet {code} rev {revision} już istnieje dla tego Klienta.");
        var package = new Package
        {
            Code = code, CustomerRevision = revision, CustomerId = request.CustomerId,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow, CreatedBy = currentUser.GetAuthor()
        };
        context.Packages.Add(package);
        context.DocumentationChangeSets.Add(Created(nameof(Package), package.Id, ChangeOperations.Created, null,
            (nameof(Package.Code), package.Code), (nameof(Package.CustomerRevision), package.CustomerRevision),
            (nameof(Package.CustomerId), package.CustomerId?.ToString()), (nameof(Package.Description), package.Description)));
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new PackageOperationException($"Pakiet {code} rev {revision} już istnieje dla tego Klienta.");
        }
        return package.Id;
    }

    public async Task<IReadOnlyList<BomRevisionSummary>> ListRevisionsAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        return await context.BomRevisions.AsNoTracking().Where(x => x.PackageId == packageId)
            .OrderByDescending(x => x.Number)
            .Select(x => new BomRevisionSummary(x.Id, x.Number, x.SourceFileName, x.SourceSheet, x.ImportedAtUtc, x.ImportedBy,
                x.Lines.Count, x.Comment))
            .ToListAsync(cancellationToken);
    }

    public async Task<BomRevisionSummary> SaveBomRevisionAsync(Guid packageId, BomImportResult import, string sourceFileName,
        string? sourceSheet, string? comment, byte[] sourceFileContent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceFileContent);
        if (sourceFileContent.LongLength == 0) throw new PackageOperationException("Plik źródłowy jest pusty.");
        if (sourceFileContent.LongLength > StoredFile.MaxSizeBytes) throw new PackageOperationException("Plik źródłowy jest za duży (max 50 MB).");
        if (import.HasErrors) throw new PackageOperationException("Import zawiera błędy blokujące. Popraw mapowanie lub plik.");
        if (import.Lines.Count == 0) throw new PackageOperationException("Brak linii do zapisania.");

        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        // Serializable: dwie osoby importujące jednocześnie nie dostaną tego samego numeru rewizji.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (!await context.Packages.AnyAsync(x => x.Id == packageId, cancellationToken))
            throw new PackageOperationException("Wybrany pakiet nie istnieje (mógł zostać usunięty).");
        var number = (await context.BomRevisions.Where(x => x.PackageId == packageId).MaxAsync(x => (int?)x.Number, cancellationToken) ?? 0) + 1;

        var now = DateTimeOffset.UtcNow;
        // Kopia pliku w bazie — wiadomo, z czego powstała rewizja (ADR-0008).
        var file = new DocumentationFile
        {
            FileName = Path.GetFileName(sourceFileName), ContentType = ContentTypeFor(sourceFileName),
            SizeBytes = sourceFileContent.LongLength, Sha256 = StoredFile.ComputeSha256(sourceFileContent), Content = sourceFileContent,
            UploadedByUserId = currentUser.UserId, UploadedBy = currentUser.GetAuthor(), UploadedAtUtc = now
        };
        context.DocumentationFiles.Add(file);
        var revision = new BomRevision
        {
            PackageId = packageId, Number = number, SourceFileName = file.FileName, SourceSheet = sourceSheet, SourceFileId = file.Id,
            ImportedAtUtc = now, ImportedBy = currentUser.GetAuthor(),
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
        };
        foreach (var source in import.Lines)
        {
            var line = new BomLine
            {
                BomRevisionId = revision.Id, LineNumber = source.LineNumber, Designators = source.Designators, Quantity = source.Quantity,
                PartTs = source.PartTs, Footprint = source.Footprint, Description = source.Description, Value = source.Value,
                PartNumber = source.PartNumber, Notes = source.Notes, Confirmed = false
            };
            line.ExpandedDesignators = source.ExpandedDesignators
                .Select(d => new BomLineDesignator { BomLineId = line.Id, BomRevisionId = revision.Id, Designator = d.Designator }).ToList();
            line.ExtraValues = source.ExtraValues
                .Select(e => new BomLineExtraValue { BomLineId = line.Id, Ordinal = e.Ordinal, Header = e.Header, Value = e.Value }).ToList();
            revision.Lines.Add(line);
            // MPN z BOM Klienta trafiają na listę zatwierdzonych z podstawą „z BOM Klienta”; pierwszy jest preferowany (2.3).
            var mpns = MpnBases.Split(source.PartNumber);
            for (var i = 0; i < mpns.Count; i++)
                context.LineMpns.Add(new LineMpn
                {
                    BomLineId = line.Id, Mpn = mpns[i], Forbidden = false, Ordinal = i + 1,
                    Note = i == 0 ? MpnBases.FromCustomerBomPreferred : MpnBases.FromCustomerBom,
                    AddedByUserId = currentUser.UserId, AddedBy = currentUser.GetAuthor(), AddedAtUtc = now
                });
        }
        context.BomRevisions.Add(revision);
        context.DocumentationChangeSets.Add(Created(nameof(Package), packageId, ChangeOperations.Imported, revision.Comment,
            ("BomRevision.Number", number.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("BomRevision.SourceFileName", file.FileName), ("BomRevision.Lines", revision.Lines.Count.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new PackageOperationException("Ktoś w tym samym czasie zapisał rewizję tego pakietu albo dane mają powtórzone desygnatory. Spróbuj ponownie.");
        }
        return new(revision.Id, revision.Number, revision.SourceFileName, revision.SourceSheet, revision.ImportedAtUtc,
            revision.ImportedBy, revision.Lines.Count, revision.Comment);
    }

    public async Task<IReadOnlyList<BomLine>> GetLinesAsync(Guid revisionId, CancellationToken cancellationToken = default)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        return await context.BomLines.AsNoTracking().Where(x => x.BomRevisionId == revisionId)
            .Include(x => x.ExpandedDesignators).Include(x => x.ExtraValues)
            .AsSplitQuery().OrderBy(x => x.LineNumber).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> ListCustomersAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        return await context.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<Guid> AddCustomerAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0) throw new PackageOperationException("Podaj nazwę Klienta.");
        if (trimmed.Length > 500) throw new PackageOperationException("Nazwa Klienta jest za długa (max 500).");
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        if (await context.Customers.AnyAsync(x => x.Name == trimmed, cancellationToken))
            throw new PackageOperationException($"Klient „{trimmed}” już istnieje na liście.");
        var customer = new Customer { Name = trimmed };
        context.Customers.Add(customer);
        context.SharedChangeSets.Add(new SharedChangeSet
        {
            UserId = currentUser.UserId, Author = currentUser.GetAuthor(), OccurredAtUtc = DateTimeOffset.UtcNow,
            SubjectType = nameof(Customer), SubjectId = customer.Id, Operation = ChangeOperations.Created,
            Changes = [new SharedFieldChange { ObjectType = nameof(Customer), ObjectId = customer.Id, Field = nameof(Customer.Name), NewValue = customer.Name }]
        });
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new PackageOperationException($"Klient „{trimmed}” już istnieje na liście.");
        }
        return customer.Id;
    }

    private DocumentationChangeSet Created(string subjectType, Guid subjectId, string operation, string? comment, params (string Field, string? Value)[] fields) => new()
    {
        UserId = currentUser.UserId, Author = currentUser.GetAuthor(), OccurredAtUtc = DateTimeOffset.UtcNow,
        SubjectType = subjectType, SubjectId = subjectId, Operation = operation, Comment = comment,
        Changes = fields.Where(f => f.Value is not null).Select(f => new DocumentationFieldChange
            { ObjectType = subjectType, ObjectId = subjectId, Field = f.Field, NewValue = f.Value }).ToList()
    };

    private static string? ContentTypeFor(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".xlsm" => "application/vnd.ms-excel.sheet.macroEnabled.12",
        ".csv" => "text/csv",
        ".txt" => "text/plain",
        _ => null
    };
}
