using System.IO;
using System.Reflection;

namespace iPart.App;

/// <summary>
/// Zapis błędów do pliku na komputerze użytkownika (pilotaż): %LOCALAPPDATA%\iPart\logs\iPart-RRRRMMDD.log.
/// Zapisujemy wersję programu, Guid konta i treść wyjątku — bez haseł, connection stringów i danych z plików Klienta.
/// Błąd zapisu dziennika nigdy nie przerywa pracy.
/// </summary>
public static class ErrorLog
{
    private static readonly object Gate = new();

    public static string Folder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "iPart", "logs");

    /// <summary>Guid wybranego konta (ustawia aplikacja po wyborze konta).</summary>
    public static Guid? AccountId { get; set; }

    public static void Write(Exception exception, string context)
    {
        try
        {
            var version = typeof(ErrorLog).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "?";
            var text = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} | wersja {version} | konto {AccountId?.ToString() ?? "—"} | {context}{Environment.NewLine}"
                       + Sanitize(exception.ToString()) + Environment.NewLine + new string('-', 80) + Environment.NewLine;
            lock (Gate)
            {
                Directory.CreateDirectory(Folder);
                File.AppendAllText(Path.Combine(Folder, $"iPart-{DateTime.Now:yyyyMMdd}.log"), text);
            }
        }
        catch
        {
            // Dziennik jest pomocniczy — jego awaria nie może zatrzymać programu.
        }
    }

    // Komunikaty SqlClient mogą zawierać fragment connection stringu — usuwamy hasło, jeśli się pojawi.
    private static string Sanitize(string text) =>
        System.Text.RegularExpressions.Regex.Replace(text, @"(?i)(password|pwd)\s*=\s*[^;\s]+", "$1=***");
}
