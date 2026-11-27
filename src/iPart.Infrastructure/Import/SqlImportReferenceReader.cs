using iPart.Core.Import;
using Microsoft.EntityFrameworkCore;

namespace iPart.Infrastructure.Import;

public sealed class SqlImportReferenceReader(IDbContextFactory<IPartDbContext> factory) : IImportReferenceReader
{
    public async Task<ImportReferences> ReadAsync(CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        var mounting = await context.MountingMethods.AsNoTracking().ToListAsync(cancellationToken);
        var qualifications = await context.AecQualifications.AsNoTracking().ToListAsync(cancellationToken);
        var keys = await context.Components.AsNoTracking()
            .Select(x => new ImportBusinessKey(x.Mpn, x.ManufacturerId)).ToListAsync(cancellationToken);
        return new(mounting.ToDictionary(x => x.Name, x => x.Id, StringComparer.Ordinal),
            qualifications.ToDictionary(x => x.Name, x => x.Id, StringComparer.Ordinal), keys.ToHashSet());
    }
}
