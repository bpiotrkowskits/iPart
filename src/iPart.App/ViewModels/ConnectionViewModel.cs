using System.Reflection;
using System.Windows.Input;
using iPart.Core;
using iPart.Core.Packages;

namespace iPart.App.ViewModels;

/// <summary>Strona „Połączenie z bazą”: wersja programu, sprawdzenie połączenia i zgodności schematu.</summary>
public sealed class ConnectionViewModel : ObservableObject
{
    private readonly IDatabaseConnectionChecker? _checker;
    private readonly IDatabaseInfoReader? _databaseInfo;
    private readonly string? _configurationError;
    private string _status = "Nie sprawdzono połączenia";
    private string _connectionDetails = "";

    public ConnectionViewModel(IDatabaseConnectionChecker? checker, string? configurationError = null,
        IDatabaseInfoReader? databaseInfo = null, string? currentUser = null)
    {
        _checker = checker;
        _configurationError = configurationError;
        _databaseInfo = databaseInfo;
        CurrentUser = currentUser ?? Environment.UserName;
        CheckConnectionCommand = new AsyncCommand(CheckConnectionAsync);
    }

    public ICommand CheckConnectionCommand { get; }
    public string CurrentUser { get; }

    public string AppVersion { get; } = "Wersja " +
        (typeof(ConnectionViewModel).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "?")
        + " — pilotaż (dane testowe)";

    public string Status { get => _status; private set => Set(ref _status, value); }

    /// <summary>Komunikat o problemie wykrytym przy starcie (np. niezgodna wersja bazy).</summary>
    public void Report(string message, string details = "") { Status = message; ConnectionDetails = details; }
    public string ConnectionDetails { get => _connectionDetails; private set => Set(ref _connectionDetails, value); }

    private async Task CheckConnectionAsync()
    {
        if (_checker is null)
        {
            Status = _configurationError ?? "Błąd konfiguracji połączenia z bazą danych.";
            return;
        }

        Status = "Sprawdzanie połączenia…";
        ConnectionDetails = "";
        try
        {
            var result = await _checker.CheckAsync();
            Status = result switch
            {
                DatabaseConnectionStatus.Connected => "Połączono z bazą danych.",
                DatabaseConnectionStatus.TimedOut => "Przekroczono limit czasu połączenia (10 sekund). Sprawdź dostępność SQL Server i spróbuj ponownie.",
                _ => "Nie udało się połączyć z bazą. Sprawdź, czy serwer SQL działa, baza istnieje i Twoje konto ma do niej dostęp."
            };
            if (result == DatabaseConnectionStatus.Connected && _databaseInfo is not null)
                ConnectionDetails = Describe(await _databaseInfo.ReadAsync());
        }
        catch (OperationCanceledException)
        {
            Status = "Sprawdzanie połączenia zostało anulowane.";
        }
        catch (Exception exception)
        {
            Status = "Połączenie działa, ale nie udało się odczytać stanu schematu: " + ErrorMessages.Describe(exception);
        }
    }

    private static string Describe(DatabaseInfo info)
    {
        var lines = new List<string>
        {
            $"Serwer: {info.Server}",
            $"Baza danych: {info.Database}",
            $"Logowanie: {info.Authentication}",
            $"Szyfrowanie połączenia: {(info.Encrypt ? "tak" : "nie")}; zaufanie do certyfikatu bez sprawdzania: {(info.TrustServerCertificate ? "TAK (tylko do testów)" : "nie")}",
            $"Zastosowane migracje schematu: {info.AppliedMigrations.Count}" + (info.AppliedMigrations.Count > 0 ? $" (ostatnia: {info.AppliedMigrations[^1]})" : ""),
        };
        lines.Add(info.PendingMigrations.Count == 0
            ? "Schemat bazy jest zgodny z tą wersją aplikacji."
            : $"UWAGA: w bazie brakuje {info.PendingMigrations.Count} migracji: {string.Join(", ", info.PendingMigrations)}. " +
              "Aplikacja ich nie stosuje sama — zrobi to administrator skryptem z folderu deploy/sql.");
        return string.Join(Environment.NewLine, lines);
    }
}
