using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using iPart.Core.Import;
using iPart.Infrastructure.Import;
using Microsoft.Win32;

namespace iPart.App.ViewModels;

public sealed class ImportExcelViewModel : INotifyPropertyChanged
{
    private const int PageSize = 200;
    private readonly IExcelPreviewService _service;
    private CancellationTokenSource? _cancellation;
    private ImportReport? _report;
    private SourceCell[] _sources = [];
    private string _filePath = "";
    private string _status = "Wybierz plik .xlsx. Analiza nie zapisuje danych do bazy.";
    private bool _isBusy;
    private bool _confirmThP, _confirmNonS;
    private int _progress, _recordPage, _issuePage, _sourcePage;
    private string _severity = "Wszystkie";
    private bool _unmappedOnly;
    private ImportPreviewRow? _selectedRecord;
    private SourceCell? _selectedSourceCell;

    public ImportExcelViewModel(IExcelPreviewService service)
    {
        _service = service;
        ChooseFileCommand = new(() =>
        {
            var dialog = new OpenFileDialog { Filter = "Skoroszyt Excel (*.xlsx)|*.xlsx", CheckFileExists = true };
            if (dialog.ShowDialog() == true)
            {
                FilePath = dialog.FileName; _report = null; _sources = []; SelectedRecord = null; SelectedSourceCell = null;
                Status = "Wybrano plik. Kliknij Analizuj plik."; RefreshPages();
            }
        }, () => !IsBusy);
        AnalyzeCommand = new RelayCommand(() => _ = AnalyzeAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(FilePath));
        CancelCommand = new(() => _cancellation?.Cancel(), () => IsBusy && _cancellation is not null);
        ExportCommand = new(() => _ = ExportAsync(), () => !IsBusy && _report is not null);
        PreviousRecordsCommand = new(() => { _recordPage--; RefreshPages(); }, () => !IsBusy && _recordPage > 0);
        NextRecordsCommand = new(() => { _recordPage++; RefreshPages(); }, () => !IsBusy && (_recordPage + 1) * PageSize < (_report?.Records.Count ?? 0));
        PreviousIssuesCommand = new(() => { _issuePage--; RefreshPages(); }, () => !IsBusy && _issuePage > 0);
        NextIssuesCommand = new(() => { _issuePage++; RefreshPages(); }, () => !IsBusy && (_issuePage + 1) * PageSize < FilteredIssues.Count());
        PreviousSourcesCommand = new(() => { _sourcePage--; RefreshPages(); }, () => !IsBusy && _sourcePage > 0);
        NextSourcesCommand = new(() => { _sourcePage++; RefreshPages(); }, () => !IsBusy && (_sourcePage + 1) * PageSize < FilteredSources.Count());
    }

    public RelayCommand ChooseFileCommand { get; }
    public RelayCommand AnalyzeCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ExportCommand { get; }
    public RelayCommand PreviousRecordsCommand { get; }
    public RelayCommand NextRecordsCommand { get; }
    public RelayCommand PreviousIssuesCommand { get; }
    public RelayCommand NextIssuesCommand { get; }
    public RelayCommand PreviousSourcesCommand { get; }
    public RelayCommand NextSourcesCommand { get; }
    public string FilePath { get => _filePath; private set { _filePath = value; Changed(); RefreshCommands(); } }
    public string Status { get => _status; private set { _status = value; Changed(); } }
    public bool IsBusy { get => _isBusy; private set { _isBusy = value; Changed(); Changed(nameof(CanChangeMapping)); RefreshCommands(); } }
    public bool CanChangeMapping => !IsBusy;
    public int Progress { get => _progress; private set { _progress = value; Changed(); } }
    public bool ConfirmThP { get => _confirmThP; set { _confirmThP = value; MappingChanged(); Changed(); } }
    public bool ConfirmNonS { get => _confirmNonS; set { _confirmNonS = value; MappingChanged(); Changed(); } }
    public string[] Severities { get; } = ["Wszystkie", "Błędy", "Ostrzeżenia", "Informacje"];
    public string Severity { get => _severity; set { _severity = value; _issuePage = 0; Changed(); RefreshPages(); } }
    public bool UnmappedOnly { get => _unmappedOnly; set { _unmappedOnly = value; _sourcePage = 0; Changed(); RefreshPages(); } }
    public ImportPreviewRow? SelectedRecord { get => _selectedRecord; set { _selectedRecord = value; Changed(); Changed(nameof(SelectedFields)); Changed(nameof(SelectedSources)); } }
    public SourceCell? SelectedSourceCell { get => _selectedSourceCell; set { _selectedSourceCell = value; Changed(); Changed(nameof(SourceDetails)); } }
    public string SourceDetails => SelectedSourceCell is not { } cell ? "Zaznacz komórkę źródłową, aby przeczytać jej pełną treść." :
        $"{cell.Location} | wiersz {cell.Row}, kolumna {cell.Column} | {cell.Header ?? "bez nagłówka"}\nOryginał:\n{cell.OriginalValue}\nZapisany wynik: {cell.Value ?? "NULL"}\nFormat: {cell.NumberFormat}\nMapowanie: {cell.Mapping ?? "niezamapowane"}";
    public IEnumerable<SourceCell> SelectedSources => SelectedRecord?.Sources ?? [];
    public IEnumerable<FieldView> SelectedFields => SelectedRecord?.FieldSources.Select(pair =>
        new FieldView(pair.Key, ProposedValue(SelectedRecord, pair.Key), string.Join(", ", pair.Value.Select(x => x.Location)))) ?? [];
    public sealed record FieldView(string Field, string? Value, string Sources);
    public IEnumerable<ImportPreviewRow> RecordPage => _report?.Records.Skip(_recordPage * PageSize).Take(PageSize) ?? [];
    public IEnumerable<ImportIssue> IssuePage => FilteredIssues.Skip(_issuePage * PageSize).Take(PageSize);
    public IEnumerable<SourceCell> SourcePage => FilteredSources.Skip(_sourcePage * PageSize).Take(PageSize);
    public IEnumerable<SourceSheet> Sheets => _report?.Sheets ?? [];
    public IEnumerable<string> UnmappedColumns => _report?.UnmappedColumns ?? [];
    public string RecordPageLabel => PageLabel(_recordPage, _report?.Records.Count ?? 0);
    public string IssuePageLabel => PageLabel(_issuePage, FilteredIssues.Count());
    public string SourcePageLabel => PageLabel(_sourcePage, FilteredSources.Count());
    public string Summary => _report is null ? "Brak wyników bieżącej analizy." :
        $"partsData: {_report.PartsDataRecords} | Gotowe (bez błędów): {_report.ReadyRecords} | Z ostrzeżeniami: {_report.WarningRecords} | Z błędami: {_report.BlockedRecords}\n" +
        $"Grupy duplikatów: {_report.DuplicateGroups} | Kolizje z bazą (rekordy): {_report.DatabaseCollisions} | Niedopasowane wiersze pomocnicze: {_report.UnmatchedAuxiliaryRows}\n" +
        $"Problemy: {_report.ProblemCount} | Dotknięte rekordy: {_report.AffectedRecords} | Niezamapowane kolumny: {_report.UnmappedColumns.Count}. Gotowe + z błędami = partsData; ostrzeżenia mogą dotyczyć obu grup.";

    private IEnumerable<ImportIssue> FilteredIssues => (_report?.Issues ?? []).Where(x => Severity switch
    { "Błędy" => x.Severity == ImportSeverity.Error, "Ostrzeżenia" => x.Severity == ImportSeverity.Warning, "Informacje" => x.Severity == ImportSeverity.Information, _ => true });
    private IEnumerable<SourceCell> FilteredSources => UnmappedOnly ? _sources.Where(x => x.Mapping is null) : _sources;

    public async Task AnalyzeAsync()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(FilePath)) return;
        using var cancellation = new CancellationTokenSource(); _cancellation = cancellation;
        IsBusy = true; Progress = 0; _report = null; _sources = []; SelectedRecord = null; SelectedSourceCell = null; RefreshPages();
        try
        {
            var progress = new Progress<ImportProgress>(p => { if (!ReferenceEquals(_cancellation, cancellation) || cancellation.IsCancellationRequested) return; Progress = p.Percent; Status = $"{p.Stage} | {p.Sheet} | odczytano/przetworzono wierszy: {p.RowsRead}"; });
            var report = await _service.AnalyzeAsync(FilePath, new(ConfirmThP, ConfirmNonS), progress, cancellation.Token);
            var sources = await Task.Run(() => report.AllSourceCells.ToArray(), cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            _report = report; _sources = sources; _recordPage = _issuePage = _sourcePage = 0;
            RefreshPages(); SelectedRecord = report.Records.FirstOrDefault(); Progress = 100;
            Status = "Analiza zakończona. Nic nie zapisano do bazy. Możesz przejrzeć dane i jawnie wyeksportować raport.";
        }
        catch (OperationCanceledException) { Status = "Analiza anulowana. Nie opublikowano częściowych wyników ani nie zapisano danych."; }
        catch (Exception exception) { Status = "Nie udało się przeanalizować pliku: " + exception.Message; }
        finally { _cancellation = null; IsBusy = false; RefreshPages(); }
    }

    private async Task ExportAsync()
    {
        if (_report is null || IsBusy) return;
        var dialog = new SaveFileDialog { Filter = "Pełny raport JSON (*.json)|*.json|Raport CSV (*.csv)|*.csv", FileName = "iPart-import-report.json", AddExtension = true };
        if (dialog.ShowDialog() != true) return;
        IsBusy = true;
        try { await Task.Run(() => ImportReportExporter.ExportAsync(_report, dialog.FileName)); Status = "Wyeksportowano raport: " + dialog.FileName; }
        catch (Exception exception) { Status = "Nie udało się wyeksportować raportu: " + exception.Message; }
        finally { IsBusy = false; }
    }
    private void MappingChanged()
    {
        _report = null; _sources = []; SelectedRecord = null; SelectedSourceCell = null;
        Status = "Zmieniono decyzję mapowania. Uruchom analizę ponownie; poprzednie wyniki unieważniono.";
        RefreshPages();
    }
    private void RefreshPages()
    {
        foreach (var name in new[] { nameof(RecordPage), nameof(IssuePage), nameof(SourcePage), nameof(Summary), nameof(Sheets), nameof(UnmappedColumns), nameof(RecordPageLabel), nameof(IssuePageLabel), nameof(SourcePageLabel) }) Changed(name);
        RefreshCommands();
    }
    private void RefreshCommands()
    {
        foreach (var property in GetType().GetProperties().Where(p => p.PropertyType == typeof(RelayCommand)))
            (property.GetValue(this) as RelayCommand)?.Refresh();
    }
    private static string PageLabel(int page, int total) => $"Strona {(total == 0 ? 0 : page + 1)} / {Math.Max(0, (total + PageSize - 1) / PageSize)} • {total} pozycji";
    private static string? ProposedValue(ImportPreviewRow record, string path)
    {
        object? value = record.Component;
        if (path == "AecQ200") return record.Component.Qualifications.Count > 0 ? "AEC-Q200" : "NULL / brak kwalifikacji";
        foreach (var part in path.Split('.')) value = value?.GetType().GetProperty(part)?.GetValue(value);
        return value is decimal number ? number.ToString("G29", CultureInfo.InvariantCulture) : value?.ToString() ?? "NULL";
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
