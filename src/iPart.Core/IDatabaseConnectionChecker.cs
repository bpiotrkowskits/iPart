namespace iPart.Core;

public interface IDatabaseConnectionChecker
{
    Task<DatabaseConnectionStatus> CheckAsync(CancellationToken cancellationToken = default);
}

public enum DatabaseConnectionStatus
{
    Connected,
    Unavailable,
    TimedOut
}
