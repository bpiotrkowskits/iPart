using System.IO;
using System.Windows;
using iPart.App.ViewModels;
using iPart.Core;
using iPart.Core.Import;
using iPart.Infrastructure;
using iPart.Infrastructure.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace iPart.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        IDatabaseConnectionChecker? checker = null;
        string? configurationError = null;
        IImportReferenceReader importReferences = new UnavailableImportReferences();
        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();
            using var configurationLifetime = configuration as IDisposable;
            var connectionString = configuration.GetConnectionString("IPartDatabase");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                configurationError = "Błąd konfiguracji: brak ConnectionStrings:IPartDatabase w appsettings.json.";
            }
            else
            {
                checker = new SqlServerConnectionChecker(connectionString);
                importReferences = new SqlImportReferenceReader(new IPartContextFactory(
                    new DbContextOptionsBuilder<IPartDbContext>().UseSqlServer(connectionString).Options));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or FormatException or InvalidOperationException or ArgumentException)
        {
            configurationError = "Błąd konfiguracji: sprawdź plik appsettings.json i wpis ConnectionStrings:IPartDatabase, a następnie uruchom aplikację ponownie.";
        }

        MainWindow = new MainWindow
        {
            DataContext = new MainWindowViewModel(checker, configurationError,
                new ImportExcelViewModel(new ExcelPreviewService(importReferences)))
        };
        MainWindow.Show();
    }

    private sealed class UnavailableImportReferences : IImportReferenceReader
    {
        public Task<ImportReferences> ReadAsync(CancellationToken cancellationToken) =>
            Task.FromException<ImportReferences>(new InvalidOperationException("Brak poprawnej konfiguracji połączenia z bazą."));
    }
}
