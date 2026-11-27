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
    public Task PackageBomShowsLinesAndConfirmsWithBasis() => OnSta(async window =>
    {
        var packages = new Packages();
        var confirm = new Confirm(packages);
        var handlers = new BomHandlers(confirm, new Ok<SetComparisonDecision>(), new Ok<RecordCustomerAnswer>(), new Ok<UpdateLineTs>(), new Ok<AddLineMpn>(),
            new Ok<RemoveLineMpn>(), new Ok<ApprovePnp>(), new Ok<ApproveBom>(), new Ok<AttachPnp>());
        var bom = new PackageBomViewModel(packages.Package, packages, new Reader(), handlers, new History(), back: () => { });
        var shell = new ShellViewModel();
        shell.Show(bom);
        window.DataContext = shell;
        await bom.LoadAsync();
        window.UpdateLayout();

        Assert.Equal(2, bom.Lines.Count);
        Assert.Contains("niepotwierdzone: 2", bom.Summary);
        bom.SelectedLine = bom.Lines[0];
        Assert.False(bom.ConfirmCommand.CanExecute(null));
        bom.SelectedBasis = bom.Bases.Single(b => b.Code == ConfirmationBases.CustomerAnswer);
        Assert.True(bom.ConfirmCommand.CanExecute(null));
        await bom.ConfirmCommand.ExecuteAsync(null);
        Assert.Equal("Potwierdzono (linia 1).", bom.Status);
        Assert.Contains("niepotwierdzone: 1", bom.Summary);

        Assert.Equal(new[] { "X9" }, bom.OnlyInPnp);
        Assert.Contains("Brak w P&P", bom.Lines.Single(l => l.LineNumber == 2).ComparisonText);
        Assert.True(bom.CanApprovePnpNow);
        bom.SelectedFilter = bom.Filters.Single(f => f.Id == "unconfirmed");
        Assert.Single(bom.Lines);
        await Rendered(window);
        Assert.Contains(Descendants(window).OfType<DataGrid>(), g => g.Items.Count == 1);
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
            new() { LineNumber = 1, Designators = "R1-R2", Quantity = 2, PartNumber = "RC0402", ExpandedDesignators = [new() { Designator = "R1" }, new() { Designator = "R2" }] },
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

    private sealed class Confirm(Packages packages) : IConfirmBomLineHandler
    {
        public Task<Result> HandleAsync(ConfirmBomLine command, CancellationToken c = default)
        {
            var line = packages.Lines.Single(l => l.Id == command.LineId);
            line.Confirmed = true;
            line.ConfirmationBasis = command.Basis;
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
            Task.FromResult<IReadOnlyList<PnpRow>>([new() { Designator = "R1" }, new() { Designator = "R2" }, new() { Designator = "X9" }]);
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
