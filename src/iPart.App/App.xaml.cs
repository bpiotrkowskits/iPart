using System.IO;
using System.Windows;
using iPart.App.ViewModels;
using iPart.Core.Shared;
using iPart.Infrastructure;
using iPart.Infrastructure.Import;
using iPart.Infrastructure.Packages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace iPart.App;

/// <summary>
/// Złożenie aplikacji (pilotaż, ADR-0014): program łączy się z bazą bezpośrednio; ViewModele korzystają tylko
/// z handlerów komend i serwisów odczytu. Start: wybór konta z listy, potem menu i strony.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Każdy nieobsłużony błąd trafia do dziennika; użytkownik dostaje komunikat zamiast zamknięcia programu.
        DispatcherUnhandledException += (_, args) =>
        {
            ErrorLog.Write(args.Exception, "nieobsłużony błąd w oknie");
            MessageBox.Show("Wystąpił nieoczekiwany błąd. Szczegóły zapisano w dzienniku błędów:" + Environment.NewLine + ErrorLog.Folder,
                "iPart", MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true;
        };
        TaskScheduler.UnobservedTaskException += (_, args) => { ErrorLog.Write(args.Exception, "nieobsłużony błąd w tle"); args.SetObserved(); };
        AppDomain.CurrentDomain.UnhandledException += (_, args) => { if (args.ExceptionObject is Exception ex) ErrorLog.Write(ex, "błąd krytyczny"); };
        var shell = new ShellViewModel();
        var session = new Session();
        MainWindow = new MainWindow { DataContext = shell };

        var configuration = ReadConfiguration(out var configurationError);
        var connectionString = configuration?.GetConnectionString("IPartDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            shell.Show(new ConnectionViewModel(null, configurationError ?? "Błąd konfiguracji: brak ConnectionStrings:IPartDatabase w appsettings.json."));
            MainWindow.Show();
            return;
        }

        var contexts = new IPartContextFactory(new DbContextOptionsBuilder<IPartDbContext>().UseSqlServer(connectionString).Options);
        var packageService = new SqlServerPackageService(contexts, session);
        var handlers = new BomHandlers(new ConfirmBomLineHandler(contexts, session), new SetComparisonDecisionHandler(contexts, session),
            new RecordCustomerAnswerHandler(contexts, session), new UpdateLineTsHandler(contexts, session), new AddLineMpnHandler(contexts, session),
            new RemoveLineMpnHandler(contexts, session), new ApprovePnpHandler(contexts, session), new ApproveBomHandler(contexts, session),
            new AttachPnpHandler(contexts, session));
        var workflow = new SqlBomWorkflowReader(contexts);
        var history = new SqlPackageHistoryReader(contexts);
        var connection = new ConnectionViewModel(new SqlServerConnectionChecker(connectionString), null,
            new SqlServerDatabaseInfoReader(connectionString, contexts), null);

        var packages = new PackagesViewModel(packageService);
        packages.Open = async (package, revisionId) =>
        {
            var bom = new PackageBomViewModel(package, packageService, workflow, handlers, history, back: () => shell.Show(packages),
                openPnpImport: b => shell.Show(new PnpImportViewModel(b, handlers.AttachPnp, back: () => shell.Show(b))),
                copy: Clipboard.SetText);
            shell.Show(bom);
            await bom.LoadAsync(revisionId);
        };
        var pages = new[]
        {
            new NavItem("Pakiety", packages),
            new NavItem("Import BOM", new BomImportViewModel(packageService, packages)),
            new NavItem("Baza komponentów", new ComponentsViewModel(new SqlServerComponentBrowser(contexts))),
            new NavItem("Import dataPart (podgląd)", new ImportExcelViewModel(new ExcelPreviewService(new SqlImportReferenceReader(contexts)))),
            new NavItem("Połączenie z bazą", connection),
        };

        var accounts = new AccountSelectionViewModel(new SqlAccountDirectory(contexts), account =>
        {
            session.SignIn(account);
            ErrorLog.AccountId = account.UserId;
            shell.SignedIn(session.AccountName, pages);
            _ = packages.RefreshAsync();
        });
        MainWindow.Show();
        _ = StartAsync(shell, accounts, connection, new SqlServerDatabaseInfoReader(connectionString, contexts));
    }

    // Przed wyborem konta: czy baza odpowiada tej wersji programu. Niezgodna baza → czytelny komunikat zamiast błędów na ekranach.
    private static async Task StartAsync(ShellViewModel shell, AccountSelectionViewModel accounts, ConnectionViewModel connection, SqlServerDatabaseInfoReader info)
    {
        try
        {
            var database = await info.ReadAsync();
            if (database.PendingMigrations.Count > 0)
            {
                connection.Report($"Baza „{database.Database}” nie jest zaktualizowana do tej wersji programu.",
                    $"Brakuje zmian struktury bazy: {string.Join(", ", database.PendingMigrations)}.{Environment.NewLine}" +
                    "Program ich nie wprowadza sam — zgłoś to osobie, która wdraża iPart (skrypt z folderu deploy/sql).");
                shell.Show(connection);
                return;
            }
        }
        catch (Exception exception)
        {
            ErrorLog.Write(exception, "sprawdzenie bazy przy starcie");
            connection.Report("Nie udało się połączyć z bazą iPart. Sprawdź sieć albo zgłoś problem osobie, która wdraża iPart.", ErrorMessages.Describe(exception));
            shell.Show(connection);
            return;
        }
        shell.Show(accounts);
        await accounts.LoadAsync();
    }

    // appsettings.json obok programu; appsettings.local.json (poza repozytorium) nadpisuje go na komputerze developera.
    private static IConfiguration? ReadConfiguration(out string? error)
    {
        error = null;
        try
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
                .Build();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or FormatException or InvalidOperationException or ArgumentException)
        {
            error = "Błąd konfiguracji: sprawdź plik appsettings.json i wpis ConnectionStrings:IPartDatabase, a następnie uruchom aplikację ponownie.";
            return null;
        }
    }
}
