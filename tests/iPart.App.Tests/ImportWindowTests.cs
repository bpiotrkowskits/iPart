using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using iPart.App;
using iPart.App.ViewModels;
using iPart.Core.Import;

namespace iPart.App.Tests;

public sealed class ImportWindowTests
{
    [Fact]
    public async Task MainWindowAndImportTabLoadWithRealWpfBindings()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            MainWindow? window = null;
            try
            {
                // Powłoka z menu (po wyborze konta) i stroną podglądu importu dataPart — nawigacja ViewModel-first.
                var shell = new ShellViewModel();
                shell.SignedIn("Konto testowe", [new NavItem("Import dataPart (podgląd)", new ImportExcelViewModel(new ImmediateService()))]);
                window = new MainWindow { DataContext = shell };
                window.Show(); window.UpdateLayout();
                Assert.Contains(Descendants(window).OfType<ListBox>(), l => l.Items.Count == 1);
                Assert.Contains(Descendants(window).OfType<Button>(), b => Equals(b.Content, "Analizuj plik"));
                Assert.Contains(Descendants(window).OfType<Button>(), b => Equals(b.Content, "Eksportuj raport…"));
                Assert.All(Descendants(window).OfType<DataGrid>(), grid => { Assert.True(grid.IsReadOnly); Assert.True(grid.EnableRowVirtualization); });
                completion.TrySetResult();
            }
            catch (Exception exception) { completion.TrySetException(exception); }
            finally { window?.Close(); System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
        thread.Join();
    }

    [Fact]
    public async Task AnalysisPublishesSummaryAndMappingChangeInvalidatesIt()
    {
        var vm = new ImportExcelViewModel(new ImmediateService());
        SetFile(vm);
        await vm.AnalyzeAsync();
        Assert.False(vm.IsBusy);
        Assert.Contains("partsData: 0", vm.Summary);
        Assert.True(vm.ExportCommand.CanExecute(null));
        vm.ConfirmThP = true;
        Assert.Contains("Brak wyników", vm.Summary);
        Assert.False(vm.ExportCommand.CanExecute(null));
    }

    [Fact]
    public async Task CancellationRestoresCommandsAndDoesNotPublishPartialResults()
    {
        var vm = new ImportExcelViewModel(new WaitingService()); SetFile(vm);
        var task = vm.AnalyzeAsync();
        Assert.True(vm.IsBusy);
        Assert.False(vm.AnalyzeCommand.CanExecute(null));
        Assert.True(vm.CancelCommand.CanExecute(null));
        vm.CancelCommand.Execute(null);
        await task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(vm.IsBusy);
        Assert.True(vm.AnalyzeCommand.CanExecute(null));
        Assert.False(vm.ExportCommand.CanExecute(null));
        Assert.Contains("anulowana", vm.Status);
    }

    private static void SetFile(ImportExcelViewModel vm) => typeof(ImportExcelViewModel).GetProperty(nameof(vm.FilePath))!.SetValue(vm, "test.xlsx");
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
    private sealed class ImmediateService : IExcelPreviewService
    {
        public Task<ImportReport> AnalyzeAsync(string path, ImportOptions options, IProgress<ImportProgress>? progress = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ImportReport { FilePath = path, InputSha256 = "test", Options = options });
    }
    private sealed class WaitingService : IExcelPreviewService
    {
        public async Task<ImportReport> AnalyzeAsync(string path, ImportOptions options, IProgress<ImportProgress>? progress = null, CancellationToken cancellationToken = default)
        { await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); throw new InvalidOperationException(); }
    }
}
