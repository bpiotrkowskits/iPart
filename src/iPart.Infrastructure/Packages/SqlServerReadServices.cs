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
