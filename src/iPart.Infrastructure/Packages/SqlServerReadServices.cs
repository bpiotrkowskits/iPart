using iPart.Core.Components;
using iPart.Core.Packages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace iPart.Infrastructure.Packages;

public sealed class SqlServerComponentBrowser(IDbContextFactory<IPartDbContext> contexts) : IComponentBrowser
{
    public async Task<(IReadOnlyList<ComponentListItem> Items, int Total)> SearchAsync(string? mpnFragment, int take, CancellationToken cancellationToken = default)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        var query = context.Components.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(mpnFragment))
        {
            var text = mpnFragment.Trim();
            query = query.Where(x => x.Mpn.Contains(text));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await (from c in query
                           join m in context.MountingMethods.AsNoTracking() on c.MountingMethodId equals m.Id into mounting
                           from m in mounting.DefaultIfEmpty()
                           orderby c.Mpn
                           select new ComponentListItem(c.Mpn, m == null ? null : m.Name, c.CaseName, c.Footprint, c.Notes))
            .Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }
}

// Informacja „z czym jestem połączony” — przydatna przy publikacji i zgłoszeniach do IT. Nigdy nie pokazuje hasła.
public sealed class SqlServerDatabaseInfoReader(string connectionString, IDbContextFactory<IPartDbContext> contexts) : IDatabaseInfoReader
{
    public async Task<DatabaseInfo> ReadAsync(CancellationToken cancellationToken = default)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var authentication = builder.IntegratedSecurity ? "Windows (konto zalogowanego użytkownika)"
            : builder.Authentication != SqlAuthenticationMethod.NotSpecified ? builder.Authentication.ToString()
            : "Login SQL Server";
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        var applied = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        return new DatabaseInfo(builder.DataSource, builder.InitialCatalog, authentication,
            builder.Encrypt != SqlConnectionEncryptOption.Optional, builder.TrustServerCertificate, applied, pending);
    }
}

/// <summary>Komponenty z bazy po MPN — kolumny rdzenia BOM w iParcie roboczym (Case, Footprint, Value z parametrów).</summary>
public sealed class SqlComponentLookup(IDbContextFactory<IPartDbContext> contexts) : IComponentLookup
{
    public async Task<IReadOnlyDictionary<string, ComponentCore>> FindAsync(IReadOnlyCollection<string> mpns, CancellationToken cancellationToken = default)
    {
        var wanted = mpns.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).Distinct().ToList();
        if (wanted.Count == 0) return new Dictionary<string, ComponentCore>();
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        var found = new List<iPart.Core.Components.Component>();
        foreach (var chunk in wanted.Chunk(500))
            found.AddRange(await context.Components.AsNoTracking()
                .Include(c => c.Resistor).Include(c => c.Capacitor).Include(c => c.Inductor).Include(c => c.Crystal)
                .Where(c => chunk.Contains(c.Mpn)).ToListAsync(cancellationToken));
        return found.GroupBy(c => c.Mpn, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => new ComponentCore(g.Key, Clean(g.First().CaseName), Clean(g.First().Footprint), ComponentValues.Describe(g.First())), StringComparer.Ordinal);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
