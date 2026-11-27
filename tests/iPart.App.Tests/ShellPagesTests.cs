using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using iPart.App;
using iPart.App.ViewModels;
using iPart.Core.Components;
using iPart.Core.Packages;
using iPart.Core.Shared;
using ProduMan.SharedKernel.Common;

namespace iPart.App.Tests;

// Strony nowej powłoki ładują się z prawdziwymi wiązaniami WPF i motywem Material Design.
public sealed class ShellPagesTests
{
    [Fact]
    public Task AccountSelectionShowsOnlySelectableAccountsWithDepartmentNames() => OnSta(async window =>
    {
        ProduManAccount? signedIn = null;
        var accounts = new AccountSelectionViewModel(new Directory(), a => signedIn = a);
        var shell = new ShellViewModel();
        shell.Show(accounts);
        window.DataContext = shell;
        await accounts.LoadAsync();
        window.UpdateLayout();

        Assert.Equal(2, accounts.Accounts.Count);
        Assert.Contains(accounts.Accounts, a => a.DepartmentText == "dział 100110 (nieznany)");
        Assert.False(accounts.ContinueCommand.CanExecute(null));
        accounts.Selected = accounts.Accounts[0];
        accounts.ContinueCommand.Execute(null);
        Assert.Equal(accounts.Accounts[0].Account.UserId, signedIn?.UserId);
        await Rendered(window);
        Assert.Contains(Descendants(window).OfType<Button>(), b => Equals(b.Content, "Dalej"));
    });

    [Fact]
    public Task PackageBomApprovesLineAndSendsOtherToDifferenceList() => OnSta(async window =>
    {
        var packages = new Packages();
        var resolve = new Resolve(packages);
        var handlers = new BomHandlers(resolve, new Decide(packages), new Ok<RecordCustomerAnswer>(), new Ok<UpdateLineTs>(), new Ok<AddLineMpn>(),
            new Ok<RemoveLineMpn>(), new Ok<ApprovePnp>(), new Ok<ApproveBom>(), new Ok<AttachPnp>());
        var bom = new PackageBomViewModel(packages.Package, packages, new Reader(), handlers, new History(), back: () => { });
        var shell = new ShellViewModel();
        shell.Show(bom);
        window.DataContext = shell;
        await bom.LoadAsync();
        window.UpdateLayout();

        Assert.Equal(2, bom.Lines.Count);
        Assert.Contains("niezatwierdzone: 2", bom.Summary);

        // Linia 1: zatwierdź — Footprint różni się od P&P, wybieramy wartość z P&P.
        bom.SelectedLine = bom.Lines.Single(l => l.LineNumber == 1);
        Assert.True(bom.IsApproveAction);
        var footprint = Assert.Single(bom.LineChoices);
        Assert.Equal(ComparedFields.Footprint, footprint.Field);
        footprint.IsPnp = true;
        await bom.ApproveLineCommand.ExecuteAsync(null);
        Assert.Equal("Zatwierdzono (linia 1).", bom.Status);
        Assert.Equal(iPart.Core.Packages.ValueSource.Pnp, resolve.Last!.Resolutions.Single().Source);
        Assert.Contains("niezatwierdzone: 1", bom.Summary);

        // Linia 2: do Wykazu Różnic z pytaniem.
        bom.SelectedLine = bom.Lines.Single(l => l.LineNumber == 2);
        bom.IsDifferenceListAction = true;
        bom.Question = "Czy U1 jest montowany?";
        await bom.AddToDifferenceListCommand.ExecuteAsync(null);
        var wrd = Assert.Single(bom.DifferenceList);
        Assert.Equal("U1", wrd.Wrd.DesignatorsBom);
        Assert.Equal("Czy U1 jest montowany?", wrd.Wrd.Question);

        // Linia w Wykazie Różnic nie da się zatwierdzić w Comparatorze — tylko w zakładce Wykaz Różnic.
        bom.SelectedLine = bom.Lines.Single(l => l.LineNumber == 2);
        Assert.False(bom.ApproveLineCommand.CanExecute(null));
        Assert.True(bom.SelectedLine!.WaitsInDifferenceList);
        await Rendered(window);
        var selectedRow = Descendants(window).OfType<DataGridRow>().Single(r => r.IsSelected && r.DataContext == bom.SelectedLine);
        Assert.Equal(((SolidColorBrush)window.FindResource("Data.Line.UnconfirmedSelected")).Color, ((SolidColorBrush)selectedRow.Background).Color);

        // Wykaz Różnic: zatwierdzenie na podstawie wcześniejszej realizacji.
        bom.SelectedWrd = wrd;
        bom.IsPreviousRealization = true;
        bom.WrdText = "Jak w poprzedniej realizacji";
        await bom.ApproveWrdCommand.ExecuteAsync(null);
        Assert.Equal(ConfirmationBases.PreviousRealization, resolve.Last!.Basis);
        Assert.Contains("niezatwierdzone: 0", bom.Summary);

        Assert.Equal("1 szt.: X9", bom.OnlyInPnpText);
        Assert.True(bom.CanApprovePnpNow);
        await Rendered(window);
        Assert.Contains(Descendants(window).OfType<DataGrid>(), g => g.Items.Count == 2);
        // Panel linii (decyzja Zatwierdź / Do Wykazu Różnic) musi się narysować — wcześniej brakowało konwertera w zasobach widoku.
        bom.SelectedLine = bom.Lines[0];
        await Rendered(window);
        Assert.Contains(Descendants(window).OfType<Button>(), b => Equals(b.Content, "Zatwierdź linię"));

        // Każda zakładka musi się narysować (błąd w szablonie wychodzi dopiero przy pokazaniu).
        // Comparator: komórka z różnicą (Footprint linii 1) ma pomarańczowe tło i pogrubienie; desygnatory P&P po przecinku.
        Assert.True(bom.Lines.Single(l => l.LineNumber == 1).CellDiffers(nameof(BomLineItem.FootprintPnp)));
        Assert.Equal("R1, R2", bom.Lines.Single(l => l.LineNumber == 1).DesignatorsPnpList);
        Assert.Contains(Descendants(window).OfType<DataGridCell>(), c => c.FontWeight == FontWeights.Bold);
        Assert.Contains(Descendants(window).OfType<iPart.App.Controls.ProcessStepper>(), s => s.Current == 3);

        for (var tab = 1; tab <= 2; tab++)
        {
            bom.ComparatorTab = tab;
            await Rendered(window);
        }
        Assert.Contains(Descendants(window).OfType<DataGrid>(), g => g.Items.Count == 1 && g.Columns.Any(c => Equals(c.Header, "X mm")));

        // Krok 4: Integracja Danych.
        bom.NextStageCommand.Execute(null);
        bom.HoveredCoreLine = bom.CoreLines[0];
        await Rendered(window);
        Assert.Contains(Descendants(window).OfType<DataGrid>(), g => g.Columns.Any(c => Equals(c.Header, "Case")));
        Assert.Contains(Descendants(window).OfType<TextBlock>(), t => t.Text == "PartNumber");
        Assert.Contains(Descendants(window).OfType<iPart.App.Controls.ProcessStepper>(), s => s.Current == 4);
        bom.NextStageCommand.Execute(null); // krok 5: Zatwierdzenie i wersje
        await Rendered(window);
        Assert.Contains(Descendants(window).OfType<iPart.App.Controls.ProcessStepper>(), s => s.Current == 5);
        Assert.Contains(Descendants(window).OfType<Button>(), b => Equals(b.Content, "Zatwierdź BOM"));
    });

    [Fact]
    public Task ImportStepsRenderWithStepperComparatorColumnAndNavigationBottomRight() => OnSta(async window =>
    {
        var packages = new Packages();
        var shell = new ShellViewModel();
        var import = new BomImportViewModel(packages);
        shell.Show(import);
        window.DataContext = shell;
        await import.RefreshAsync();
        await Rendered(window);
        Assert.Contains(Descendants(window).OfType<iPart.App.Controls.ProcessStepper>(), s => s.Current == 1);
        Assert.Contains(Descendants(window).OfType<DataGrid>(), g => g.Columns.Any(c => Equals(c.Header, "Comparator")));
        Assert.Contains(Descendants(window).OfType<Button>(), b => Equals(b.Content, "Dalej: Import P&P  →"));
        Assert.DoesNotContain(Descendants(window).OfType<Button>(), b => Equals(b.Content, "Utwórz pakiet"));

        var handlers = new BomHandlers(new Ok<ResolveLine>(), new Ok<SetComparisonDecision>(), new Ok<RecordCustomerAnswer>(), new Ok<UpdateLineTs>(), new Ok<AddLineMpn>(),
            new Ok<RemoveLineMpn>(), new Ok<ApprovePnp>(), new Ok<ApproveBom>(), new Ok<AttachPnp>());
        var bom = new PackageBomViewModel(packages.Package, packages, new Reader(), handlers, new History(), back: () => { });
        var pnp = new PnpImportViewModel(bom, handlers.AttachPnp, back: () => { }, backToBom: () => Task.CompletedTask, confirm: _ => false);
        shell.Show(pnp);
        await Rendered(window);
        Assert.Contains(Descendants(window).OfType<iPart.App.Controls.ProcessStepper>(), s => s.Current == 2);
        Assert.Contains(Descendants(window).OfType<Button>(), b => Equals(b.Content, "← Wróć do kroku 1") && b.IsVisible);
        Assert.Contains(Descendants(window).OfType<Button>(), b => Equals(b.Content, "Dalej: Comparator  →"));
        var row = new PnpColumnRow { Field = PnpField.Footprint };
        Assert.True(row.Compare);
        row.Field = PnpField.X;
        Assert.False(row.Compare);
    });

    private static async Task OnSta(Func<MainWindow, Task> test)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            var window = new MainWindow();
            window.Show();
            dispatcher.InvokeAsync(async () =>
            {
                try { await test(window); completion.TrySetResult(); }
                catch (Exception exception) { completion.TrySetException(exception); }
                finally { window.Close(); dispatcher.InvokeShutdown(); }
            });
            System.Windows.Threading.Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(20));
        thread.Join();
    }

    private static async Task Rendered(Window window)
    {
        await window.Dispatcher.InvokeAsync(window.UpdateLayout, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private sealed class Directory : IAccountDirectory
    {
        public Task<IReadOnlyList<ProduManAccount>> ListSelectableAsync(CancellationToken c = default) =>
            Task.FromResult<IReadOnlyList<ProduManAccount>>([
                new() { UserId = Guid.NewGuid(), Login = "a", FullName = "Konto A", Department = 100108, IsActive = true },
                new() { UserId = Guid.NewGuid(), Login = "b", FullName = "Konto B", Department = 100110, IsActive = true }]);
        public Task<IReadOnlyDictionary<int, Department>> DepartmentsAsync(CancellationToken c = default) =>
            Task.FromResult<IReadOnlyDictionary<int, Department>>(new Dictionary<int, Department> { [100108] = new() { Id = 100108, Name = "Zakupy" } });
    }

    private sealed class Packages : IPackageService
    {
        public PackageSummary Package { get; } = new(Guid.NewGuid(), "PCBA-TEST", "A", "K-001", null, 1, DateTimeOffset.UtcNow, "test");
        private readonly BomRevisionSummary _revision = new(Guid.NewGuid(), 1, "bom.xlsx", "BOM", DateTimeOffset.UtcNow, "test", 2, null);
        public List<BomLine> Lines { get; } =
        [
            new() { LineNumber = 1, Designators = "R1-R2", Quantity = 2, PartNumber = "RC0402", Footprint = "0402", ExpandedDesignators = [new() { Designator = "R1" }, new() { Designator = "R2" }] },
            new() { LineNumber = 2, Designators = "U1", Quantity = 1, PartNumber = "STM32", ExpandedDesignators = [new() { Designator = "U1" }] },
        ];
        public Task<IReadOnlyList<BomRevisionSummary>> ListRevisionsAsync(Guid id, CancellationToken c = default) => Task.FromResult<IReadOnlyList<BomRevisionSummary>>([_revision]);
        public Task<IReadOnlyList<BomLine>> GetLinesAsync(Guid id, CancellationToken c = default) => Task.FromResult<IReadOnlyList<BomLine>>(Lines);
        public Task<IReadOnlyList<PackageSummary>> ListPackagesAsync(string? s, CancellationToken c = default) => throw new NotSupportedException();
        public Task<Guid> CreatePackageAsync(NewPackageRequest r, CancellationToken c = default) => throw new NotSupportedException();
        public Task<BomRevisionSummary> SaveBomRevisionAsync(Guid id, BomImportResult i, string f, string? s, string? m, byte[] b, CancellationToken c = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Customer>> ListCustomersAsync(CancellationToken c = default) => throw new NotSupportedException();
        public Task<Guid> AddCustomerAsync(string n, CancellationToken c = default) => throw new NotSupportedException();
    }

    private sealed class Resolve(Packages packages) : ICommandHandler<ResolveLine>
    {
        public ResolveLine? Last { get; private set; }
        public Task<Result> HandleAsync(ResolveLine command, CancellationToken c = default)
        {
            Last = command;
            var line = packages.Lines.Single(l => l.Id == command.LineId);
            line.Confirmed = true;
            line.ConfirmationBasis = command.Basis;
            return Task.FromResult(Result.Ok());
        }
    }

    private sealed class Decide(Packages packages) : ICommandHandler<SetComparisonDecision>
    {
        public Task<Result> HandleAsync(SetComparisonDecision command, CancellationToken c = default)
        {
            var line = packages.Lines.Single(l => l.Id == command.LineId);
            line.ComparisonDecision = command.Decision;
            line.DifferenceQuestion = command.Question;
            return Task.FromResult(Result.Ok());
        }
    }

    private sealed class Ok<T> : ICommandHandler<T>
    {
        public Task<Result> HandleAsync(T command, CancellationToken c = default) => Task.FromResult(Result.Ok());
    }

    // P&P bez U1 → linia 2 ma rozbieżność „Brak w P&P”.
    private sealed class Reader : IBomWorkflowReader
    {
        public Task<BomRevision?> RevisionAsync(Guid id, CancellationToken c = default) => Task.FromResult<BomRevision?>(new BomRevision { Id = id, PnpFileName = "pnp.txt" });
        public Task<IReadOnlyList<PnpRow>> PnpRowsAsync(Guid id, CancellationToken c = default) =>
            Task.FromResult<IReadOnlyList<PnpRow>>([new() { Designator = "R1", Footprint = "R0402" }, new() { Designator = "R2", Footprint = "R0402" }, new() { Designator = "X9" }]);
        public Task<IReadOnlyList<LineMpn>> MpnsAsync(Guid id, CancellationToken c = default) => Task.FromResult<IReadOnlyList<LineMpn>>([]);
        public Task<IReadOnlyList<BomVersion>> VersionsAsync(Guid id, CancellationToken c = default) => Task.FromResult<IReadOnlyList<BomVersion>>([]);
        public Task<IReadOnlyList<MountingOption>> MountingMethodsAsync(CancellationToken c = default) => Task.FromResult<IReadOnlyList<MountingOption>>([new("SMT", "SMT"), new("THT", "THT")]);
    }

    private sealed class History : IPackageHistoryReader
    {
        public Task<IReadOnlyList<HistoryEntry>> ListAsync(Guid id, CancellationToken c = default) =>
            Task.FromResult<IReadOnlyList<HistoryEntry>>([new(DateTimeOffset.UtcNow, "test", "import BOM", null, ["BomRevision.Number: 1"])]);
    }
}
