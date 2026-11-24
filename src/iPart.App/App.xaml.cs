using System.IO;
using System.Windows;
using iPart.App.ViewModels;
using iPart.Core;
using iPart.Core.Components;
using iPart.Core.Import;
using iPart.Core.Packages;
using iPart.Infrastructure;
using iPart.Infrastructure.Import;
using iPart.Infrastructure.Packages;
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
        IPackageService packageService = new UnavailablePackages();
        IComponentBrowser componentBrowser = new UnavailablePackages();
        IDatabaseInfoReader? databaseInfo = null;
        var currentUser = new WindowsCurrentUser();
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
                var contexts = new IPartContextFactory(
                    new DbContextOptionsBuilder<IPartDbContext>().UseSqlServer(connectionString).Options);
                importReferences = new SqlImportReferenceReader(contexts);
                packageService = new SqlServerPackageService(contexts, currentUser);
                componentBrowser = new SqlServerComponentBrowser(contexts);
                databaseInfo = new SqlServerDatabaseInfoReader(connectionString, contexts);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or FormatException or InvalidOperationException or ArgumentException)
        {
            configurationError = "Błąd konfiguracji: sprawdź plik appsettings.json i wpis ConnectionStrings:IPartDatabase, a następnie uruchom aplikację ponownie.";
        }

        var packages = new PackagesViewModel(packageService);
        MainWindow = new MainWindow
        {
            DataContext = new MainWindowViewModel(checker, configurationError,
                new ImportExcelViewModel(new ExcelPreviewService(importReferences)),
                packages, new BomImportViewModel(packageService, packages), new ComponentsViewModel(componentBrowser),
                databaseInfo, currentUser.GetAuthor())
        };
        MainWindow.Show();
        if (checker is not null) _ = packages.RefreshAsync();
    }

    // Używane, gdy konfiguracja połączenia jest błędna: każda operacja zwraca czytelny komunikat.
    private sealed class UnavailablePackages : IPackageService, IComponentBrowser
    {
        private static Task<T> Fail<T>() => Task.FromException<T>(new PackageOperationException("Brak poprawnej konfiguracji połączenia z bazą (appsettings.json)."));
        public Task<IReadOnlyList<PackageSummary>> ListPackagesAsync(string? search, CancellationToken c = default) => Fail<IReadOnlyList<PackageSummary>>();
        public Task<Guid> CreatePackageAsync(NewPackageRequest r, CancellationToken c = default) => Fail<Guid>();
        public Task<IReadOnlyList<BomRevisionSummary>> ListRevisionsAsync(Guid id, CancellationToken c = default) => Fail<IReadOnlyList<BomRevisionSummary>>();
        public Task<BomRevisionSummary> SaveBomRevisionAsync(Guid id, BomImportResult i, string f, string? s, string? m, CancellationToken c = default) => Fail<BomRevisionSummary>();
        public Task<IReadOnlyList<BomLine>> GetLinesAsync(Guid id, CancellationToken c = default) => Fail<IReadOnlyList<BomLine>>();
        public Task<IReadOnlyList<Customer>> ListCustomersAsync(CancellationToken c = default) => Fail<IReadOnlyList<Customer>>();
        public Task<int> AddCustomerAsync(string n, CancellationToken c = default) => Fail<int>();
        public Task<(IReadOnlyList<ComponentListItem> Items, int Total)> SearchAsync(string? m, int t, CancellationToken c = default) => Fail<(IReadOnlyList<ComponentListItem>, int)>();
    }

    private sealed class UnavailableImportReferences : IImportReferenceReader
    {
        public Task<ImportReferences> ReadAsync(CancellationToken cancellationToken) =>
            Task.FromException<ImportReferences>(new InvalidOperationException("Brak poprawnej konfiguracji połączenia z bazą."));
    }
}
