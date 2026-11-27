namespace iPart.Tests;

// Plik konfiguracji dla testów na prawdziwej bazie. Domyślnie konfiguracja aplikacji;
// IPART_TEST_SETTINGS wskazuje inny plik (np. bazę LocalDB developera), bez zmiany plików w repozytorium.
internal static class TestSettings
{
    public static string Path(string repositoryRoot) =>
        Environment.GetEnvironmentVariable("IPART_TEST_SETTINGS") is { Length: > 0 } custom
            ? custom
            : System.IO.Path.Combine(repositoryRoot, "src/iPart.App/appsettings.json");
}
