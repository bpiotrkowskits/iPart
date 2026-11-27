using System.Collections.ObjectModel;
using System.IO;
using iPart.Core.Import;
using iPart.Core.Packages;
using iPart.Infrastructure.Import;
using iPart.Infrastructure.Packages;
using Microsoft.Win32;

namespace iPart.App.ViewModels;

public sealed record FieldOption(BomField Field, string Label);

public sealed class ColumnMappingRow : ObservableObject
{
    private BomField _field;
    private int _priority = 1;
    public required int Index { get; init; }
    public string Letter => OpenXmlWorkbookReader.ColumnName(Index + 1);
    public required string Header { get; init; }
    public required string Sample { get; init; }
    public BomField Field { get => _field; set { if (Set(ref _field, value)) Changed?.Invoke(); } }
    public int Priority { get => _priority; set { if (Set(ref _priority, Math.Max(1, value))) Changed?.Invoke(); } }
    public event Action? Changed;
}

public sealed class BomImportViewModel : ObservableObject
{
    private readonly IPackageService _service;
    private IReadOnlyList<WorkbookSheetGrid> _sheets = [];
    private WorkbookSheetGrid? _selectedSheet;
    private BomImportResult? _result;
    private BomSourceTable? _table;
    private string _filePath = "", _comment = "", _summary = "";
    private byte[]? _fileContent;
    private string _status = "1) Wybierz pakiet  2) wybierz plik BOM (.xlsx)  3) przypisz kolumny  4) Podgląd  5) Zapisz.";
    private int _headerRow = 1;
    private bool _isBusy;
    private PackageSummary? _selectedPackage;

    public BomImportViewModel(IPackageService service, PackagesViewModel packages)
    {
        _service = service;
        PackageList = packages;
        ChooseFileCommand = new AsyncCommand(ChooseFileAsync);
        SuggestCommand = new RelayCommand(SuggestByHeaders, () => Columns.Count > 0);
        PreviewCommand = new RelayCommand(Preview, () => _table is not null);
        SaveCommand = new AsyncCommand(SaveAsync);
        FieldOptions = Enum.GetValues<BomField>().Select(f => new FieldOption(f, BomFields.Label(f))).ToList();
    }

    public PackagesViewModel PackageList { get; }
    public IReadOnlyList<FieldOption> FieldOptions { get; }
    public ObservableCollection<WorkbookSheetGrid> Sheets { get; } = [];
    public ObservableCollection<ColumnMappingRow> Columns { get; } = [];
    public ObservableCollection<BomLineItem> PreviewLines { get; } = [];
    public ObservableCollection<BomImportIssue> Issues { get; } = [];
    public AsyncCommand ChooseFileCommand { get; }
    public RelayCommand SuggestCommand { get; }
    public RelayCommand PreviewCommand { get; }
    public AsyncCommand SaveCommand { get; }

    public string FilePath { get => _filePath; private set => Set(ref _filePath, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public string Summary { get => _summary; private set => Set(ref _summary, value); }
    public string Comment { get => _comment; set => Set(ref _comment, value); }
    public bool IsBusy { get => _isBusy; private set => Set(ref _isBusy, value); }
    public PackageSummary? SelectedPackage { get => _selectedPackage; set => Set(ref _selectedPackage, value); }

    public WorkbookSheetGrid? SelectedSheet
    {
        get => _selectedSheet;
        set
        {
            if (!Set(ref _selectedSheet, value) || value is null) return;
            // Domyślny wiersz nagłówków: pierwszy wiersz z co najmniej dwiema wypełnionymi komórkami.
            var index = value.Rows.ToList().FindIndex(r => r.Values.Count(v => !string.IsNullOrWhiteSpace(v)) >= 2);
            // Powtórzony identyczny wiersz nagłówków (np. tytuł tabeli Excela) — bierzemy ostatni z nich.
            while (index >= 0 && index + 1 < value.Rows.Count && RepeatsHeader(value.Rows[index].Values, value.Rows[index + 1].Values))
                index++;
            HeaderRow = index >= 0 ? value.Rows[index].RowNumber : 1;
            LoadColumns();
        }
    }

    public int HeaderRow
    {
        get => _headerRow;
        set { if (Set(ref _headerRow, Math.Max(1, value))) LoadColumns(); }
    }

    // Następny wiersz powtarza nagłówek, gdy każda jego niepusta komórka jest taka sama jak w nagłówku.
    private static bool RepeatsHeader(IReadOnlyList<string?> header, IReadOnlyList<string?> next)
    {
        var filled = Enumerable.Range(0, next.Count).Where(i => !string.IsNullOrWhiteSpace(next[i])).ToList();
        return filled.Count >= 2 && filled.All(i => i < header.Count &&
            string.Equals(header[i]?.Trim(), next[i]!.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private async Task ChooseFileAsync()
    {
        var dialog = new OpenFileDialog { Filter = "Skoroszyt Excel (*.xlsx)|*.xlsx", CheckFileExists = true, Title = "Wybierz BOM Klienta" };
        if (dialog.ShowDialog() != true) return;
        IsBusy = true;
        Status = "Odczyt pliku…";
        try
        {
            var path = dialog.FileName;
            var content = await File.ReadAllBytesAsync(path);
            _sheets = await Task.Run(() => BomWorkbookReader.Read(content));
            _fileContent = content;
            FilePath = path;
            Sheets.Clear();
            foreach (var s in _sheets.Where(s => s.Rows.Count > 0)) Sheets.Add(s);
            SelectedSheet = Sheets.FirstOrDefault();
            Status = Sheets.Count == 0 ? "Plik nie zawiera danych." : "Wybierz arkusz i wiersz nagłówków, potem przypisz kolumny do pól.";
        }
        catch (Exception e) { Status = ErrorMessages.Describe(e); }
        finally { IsBusy = false; }
    }

    private void LoadColumns()
    {
        Invalidate();
        Columns.Clear();
        if (SelectedSheet is not { } sheet) { _table = null; return; }
        _table = sheet.ToTable(HeaderRow);
        for (var i = 0; i < _table.Headers.Count; i++)
        {
            var index = i;
            var samples = _table.Rows.Select(r => index < r.Values.Count ? r.Values[index] : null)
                .Where(v => !string.IsNullOrWhiteSpace(v)).Take(3).Select(v => v!.Length > 40 ? v[..40] + "…" : v);
            var row = new ColumnMappingRow { Index = i, Header = _table.Headers[i], Sample = string.Join(" | ", samples) };
            row.Changed += Invalidate;
            Columns.Add(row);
        }
        SuggestCommand.Refresh();
        PreviewCommand.Refresh();
    }

    // Tylko na wyraźne polecenie użytkownika i tylko przy dokładnej nazwie nagłówka (bez zgadywania).
    private void SuggestByHeaders()
    {
        foreach (var column in Columns)
        {
            var match = FieldOptions.FirstOrDefault(f => f.Field != BomField.None &&
                string.Equals(BomFields.Label(f.Field), column.Header.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match is not null && column.Field == BomField.None) column.Field = match.Field;
        }
        Status = "Przypisano kolumny o nagłówkach identycznych z nazwami pól. Sprawdź i uzupełnij resztę.";
    }

    private void Preview()
    {
        if (_table is null) return;
        _result = BomImportBuilder.Build(_table, Columns.Select(c => new ColumnAssignment(c.Index, c.Field, c.Priority)).ToList());
        PreviewLines.Clear();
        foreach (var line in _result.Lines) PreviewLines.Add(new BomLineItem(line));
        Issues.Clear();
        foreach (var issue in _result.Issues.OrderByDescending(i => i.Severity).ThenBy(i => i.LineNumber)) Issues.Add(issue);
        var errors = _result.Issues.Count(i => i.Severity == ImportSeverity.Error);
        var warnings = _result.Issues.Count(i => i.Severity == ImportSeverity.Warning);
        Summary = $"Linii: {_result.Lines.Count}, desygnatorów po rozbiciu: {_result.DesignatorCount}, błędów: {errors}, ostrzeżeń: {warnings}.";
        Status = errors > 0 ? "Są błędy blokujące — popraw mapowanie albo plik i kliknij Podgląd ponownie."
            : "Podgląd gotowy. Wybierz pakiet i kliknij „Zapisz jako nową rewizję importu”.";
    }

    private async Task SaveAsync()
    {
        if (_result is null) { Status = "Najpierw kliknij Podgląd."; return; }
        if (SelectedPackage is not { } package) { Status = "Wybierz pakiet, do którego należy ten BOM."; return; }
        if (_result.HasErrors) { Status = "Nie można zapisać — podgląd zawiera błędy blokujące."; return; }
        if (_fileContent is null) { Status = "Najpierw wybierz plik BOM."; return; }
        IsBusy = true;
        try
        {
            var saved = await _service.SaveBomRevisionAsync(package.Id, _result, FilePath, SelectedSheet?.Name, Comment, _fileContent);
            Status = $"Zapisano {saved.DisplayName} dla {package.DisplayName}: {saved.LineCount} linii.";
            Comment = "";
            await PackageList.ShowRevisionAsync(package.Id, saved.Id);
            SelectedPackage = PackageList.Packages.FirstOrDefault(p => p.Id == package.Id);
            Invalidate(keepStatus: true);
        }
        catch (Exception e) { Status = ErrorMessages.Describe(e); }
        finally { IsBusy = false; }
    }

    private void Invalidate() => Invalidate(false);

    private void Invalidate(bool keepStatus)
    {
        if (_result is null && PreviewLines.Count == 0) return;
        _result = null;
        PreviewLines.Clear();
        Issues.Clear();
        Summary = "";
        if (!keepStatus) Status = "Mapowanie zmienione — kliknij Podgląd.";
    }
}
