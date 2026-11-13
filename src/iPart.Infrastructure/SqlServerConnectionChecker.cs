using iPart.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace iPart.Infrastructure;

public sealed class SqlServerConnectionChecker : IDatabaseConnectionChecker
{
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(10);
    private readonly DbContextOptions<IPartDbContext> _options;

    public SqlServerConnectionChecker(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var settings = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(settings.DataSource) ||
            string.IsNullOrWhiteSpace(settings.InitialCatalog))
        {
            throw new ArgumentException("Connection string must specify a server and database.", nameof(connectionString));
        }

        // Bound the provider timeout too, including zero (infinite) in configuration.
        settings.ConnectTimeout = settings.ConnectTimeout == 0
            ? (int)CheckTimeout.TotalSeconds
            : Math.Min(settings.ConnectTimeout, (int)CheckTimeout.TotalSeconds);
        _options = new DbContextOptionsBuilder<IPartDbContext>()
            .UseSqlServer(settings.ConnectionString)
            .Options;
    }

    public async Task<DatabaseConnectionStatus> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(CheckTimeout);
        try
        {
            // Keep synchronous LocalDB/provider initialization off the UI thread.
            return await Task.Run(async () =>
            {
                await using var context = new IPartDbContext(_options);
                var connected = await context.Database.CanConnectAsync(timeout.Token).ConfigureAwait(false);
                // CanConnectAsync can return false instead of propagating cancellation.
                timeout.Token.ThrowIfCancellationRequested();
                return connected ? DatabaseConnectionStatus.Connected : DatabaseConnectionStatus.Unavailable;
            }, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DatabaseConnectionStatus.TimedOut;
        }
    }
}
