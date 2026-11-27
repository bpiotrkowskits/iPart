using System.IO;
using System.Windows;
using iPart.App.ViewModels;
using iPart.Core.Packages;
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
            var text = "Wystąpił nieoczekiwany błąd. Szczegóły zapisano w dzienniku błędów:" + Environment.NewLine + ErrorLog.Folder;
            try { Controls.AppDialog.Inform(text); }
            catch { MessageBox.Show(text, "iPart", MessageBoxButton.OK, MessageBoxImage.Warning); } // awaryjnie, gdyby okno aplikacji nie dało się pokazać
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
        var handlers = new BomHandlers(new ResolveLineHandler(contexts, session), new SetComparisonDecisionHandler(contexts, session),
            new RecordCustomerAnswerHandler(contexts, session), new UpdateLineTsHandler(contexts, session), new AddLineMpnHandler(contexts, session),
            new RemoveLineMpnHandler(contexts, session), new ApprovePnpHandler(contexts, session), new ApproveBomHandler(contexts, session),
            new AttachPnpHandler(contexts, session), new SetComparatorOptionsHandler(contexts, session), new SplitLineHandler(contexts, session),
            new ResetComparatorHandler(contexts, session));
        var discard = new DiscardBomRevisionHandler(contexts, session);
        var workflow = new SqlBomWorkflowReader(contexts);
        var history = new SqlPackageHistoryReader(contexts);
        var components = new SqlComponentLookup(contexts);
        var connection = new ConnectionViewModel(new SqlServerConnectionChecker(connectionString), null,
            new SqlServerDatabaseInfoReader(connectionString, contexts), null);

        var packages = new PackagesViewModel(packageService);
        BomImportViewModel? import = null;
        // Ekran pakietu (krok 3 Comparator, krok 4 Integracja Danych). backToBom — tylko w przebiegu importu: powrót do kroku 1 usuwa wczytany BOM.
        PackageBomViewModel Workspace(PackageSummary package, bool fromImport)
        {
            PackageBomViewModel? bom = null;
            bom = new(package, packageService, workflow, handlers, history, back: () => { shell.Show(packages); _ = packages.RefreshAsync(); },
                openPnpImport: b => ShowPnp(b, fromImport),
                workbook: new DifferenceListWorkbook(), files: ExcelDialogs, components: components, confirm: Confirm);
            return bom;
        }
        void ShowPnp(PackageBomViewModel bom, bool fromImport) =>
            shell.Show(new PnpImportViewModel(bom, handlers.AttachPnp, back: () => shell.Show(bom),
                backToBom: !fromImport ? null : async () =>
                {
                    if (bom.SelectedRevision is { } revision)
                    {
                        var result = await discard.HandleAsync(new DiscardBomRevision(revision.Id));
                        if (!result.IsSuccess) { Controls.AppDialog.Inform(result.Error ?? "Nie udało się wrócić do kroku 1."); return; }
                    }
                    if (import is not null) shell.Show(import);
                },
                confirm: Confirm));
        packages.Open = async (package, revisionId) =>
        {
            var bom = Workspace(package, fromImport: false);
            shell.Show(bom);
            await bom.LoadAsync(revisionId);
        };
        // Import BOM/P&P: 1 BOM → (zapis w tle, wybór pól do porównania) → 2 Import P&P → 3 Comparator → 4 Integracja Danych.
        import = new BomImportViewModel(packageService, async (package, revisionId, compare) =>
        {
            await handlers.SetOptions!.HandleAsync(new SetComparatorOptions(revisionId, compare));
            var bom = Workspace(package, fromImport: true);
            await bom.LoadAsync(revisionId);
            ShowPnp(bom, fromImport: true);
        });
        var pages = new[]
        {
            new NavItem("Pakiet Elektroniczny", packages),
            new NavItem("Import BOM/P&P", import!),
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
            _ = import.RefreshAsync();
        });
        connection.Report("Łączenie z bazą iPart…");
        shell.Show(connection);
        MainWindow.Show();
        _ = StartAsync(shell, accounts, connection, new SqlServerDatabaseInfoReader(connectionString, contexts));
    }

    // Przed wyborem konta: czy baza odpowiada tej wersji programu. Niezgodna baza → czytelny komunikat zamiast błędów na ekranach.
    private static async Task StartAsync(ShellViewModel shell, AccountSelectionViewModel accounts, ConnectionViewModel connection, SqlServerDatabaseInfoReader info)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var database = await info.ReadAsync(timeout.Token);
            if (database.PendingMigrations.Count > 0)
            {
                connection.Report($"Baza „{database.Database}” nie jest zaktualizowana do tej wersji programu.",
                    $"Brakuje zmian struktury bazy: {string.Join(", ", database.PendingMigrations)}.{Environment.NewLine}" +
                    "Program ich nie wprowadza sam — zgłoś to osobie, która wdraża iPart (skrypt z folderu deploy/sql).");
                shell.Show(connection);
                return;
            }
        }
        catch (OperationCanceledException)
        {
            connection.Report("Baza iPart nie odpowiada (20 s). Zamknij program i uruchom go ponownie za chwilę; jeśli to się powtarza — zgłoś problem osobie, która wdraża iPart.");
            return;
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

    // Okna zapisu i wyboru pliku Excel (Wykaz Różnic) — ViewModel dostaje je jako funkcje.
    // Potwierdzenie przed cofnięciem kroku (zmiany przepadają).
    private static bool Confirm(string message) =>
        Controls.AppDialog.Confirm(message);

    private static readonly FileDialogs ExcelDialogs = new(
        (name, content) =>
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { FileName = name, Filter = "Skoroszyt Excel (*.xlsx)|*.xlsx", Title = "Zapisz Wykaz Różnic" };
            if (dialog.ShowDialog() != true) return false;
            File.WriteAllBytes(dialog.FileName, content);
            return true;
        },
        () =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Skoroszyt Excel (*.xlsx)|*.xlsx", CheckFileExists = true, Title = "Wybierz Wykaz Różnic z odpowiedziami Klienta" };
            return dialog.ShowDialog() == true ? File.ReadAllBytes(dialog.FileName) : null;
        });

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
