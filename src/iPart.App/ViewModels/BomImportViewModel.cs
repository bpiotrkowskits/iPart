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
    private bool _compare;
    public required int Index { get; init; }
    public string Letter => OpenXmlWorkbookReader.ColumnName(Index + 1);
    public required string Header { get; init; }
    public required string Sample { get; init; }
    public BomField Field
    {
        get => _field;
        set
        {
            if (!Set(ref _field, value)) return;
            OnPropertyChanged(nameof(CanCompare));
            Compare = CanCompare && value != BomField.PartNumber; // Value, Footprint, Description — domyślnie zaznaczone; PartNumber — na życzenie
            Changed?.Invoke();
        }
    }
    /// <summary>Kolumna „Comparator”: porównać tę kolumnę z P&amp;P (tylko Value, Footprint, Description, PartNumber).</summary>
    public bool CanCompare => Field is BomField.Value or BomField.Footprint or BomField.Description or BomField.PartNumber;
    public bool Compare { get => _compare && CanCompare; set => Set(ref _compare, value); }
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
    private string _status = "1) Wybierz Klienta  2) wybierz pakiet  3) wybierz plik BOM (.xlsx) i przypisz kolumny  4) Podgląd  5) Dalej: Import P&P.";
    private int _headerRow = 1;
    private bool _isBusy;
    private PackageSummary? _selectedPackage;
    private CustomerOption? _selectedCustomer;
    private IReadOnlyList<PackageSummary> _allPackages = [];
    private readonly Func<PackageSummary, Guid, ComparatorOptions, Task>? _continueToPnp;

    /// <param name="continueToPnp">Krok dalej: Import P&amp;P dla zapisanego BOM (pakiet, rewizja importu).</param>
    public BomImportViewModel(IPackageService service, Func<PackageSummary, Guid, ComparatorOptions, Task>? continueToPnp = null)
    {
        _service = service;
        _continueToPnp = continueToPnp;
        ChooseFileCommand = new AsyncCommand(ChooseFileAsync);
        SuggestCommand = new RelayCommand(SuggestByHeaders, () => Columns.Count > 0);
        PreviewCommand = new RelayCommand(Preview, () => _table is not null);
        SaveCommand = new AsyncCommand(SaveAsync);
        RefreshCommand = new AsyncCommand(RefreshAsync);
        // Na tym etapie nie przypisujemy Part TS — porównujemy Value z P&P.
        FieldOptions = Enum.GetValues<BomField>().Where(f => f != BomField.PartTs).Select(f => new FieldOption(f, BomFields.Label(f))).ToList();
    }

    public IReadOnlyList<FieldOption> FieldOptions { get; }
    public ObservableCollection<CustomerOption> Customers { get; } = [];
    public ObservableCollection<PackageSummary> Packages { get; } = [];
    public AsyncCommand RefreshCommand { get; }

    /// <summary>Krok 1: Klient. Lista pakietów (krok 2) pokazuje tylko pakiety tego Klienta.</summary>
    public CustomerOption? SelectedCustomer
    {
        get => _selectedCustomer;
        set { if (Set(ref _selectedCustomer, value)) FilterPackages(null); }
    }

    public async Task RefreshAsync()
    {
        try
        {
            var customer = SelectedCustomer?.Id;
            var package = SelectedPackage?.Id;
            _allPackages = await _service.ListPackagesAsync(null);
            var customers = await _service.ListCustomersAsync();
            Customers.Clear();
            Customers.Add(new CustomerOption(null, "(bez Klienta)"));
            foreach (var c in customers) Customers.Add(new CustomerOption(c.Id, c.Name));
            _selectedCustomer = null;
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == customer && customer is not null);
            FilterPackages(package);
        }
        catch (Exception e) { Status = ErrorMessages.Describe(e); }
    }

    private void FilterPackages(Guid? select)
    {
        Packages.Clear();
        if (SelectedCustomer is { } customer)
            foreach (var p in _allPackages.Where(p => p.CustomerId == customer.Id)) Packages.Add(p);
        SelectedPackage = Packages.FirstOrDefault(p => p.Id == select);
    }

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

    // Tylko na wyraźne polecenie użytkownika: nagłówek identyczny z nazwą pola albo jedną z jego typowych nazw.
    private void SuggestByHeaders()
    {
        foreach (var column in Columns)
        {
            var header = column.Header.Trim();
            var match = FieldOptions.FirstOrDefault(f => f.Field != BomField.None &&
                (string.Equals(BomFields.Label(f.Field), header, StringComparison.OrdinalIgnoreCase) || Synonym(header) == f.Field));
            if (match is not null && column.Field == BomField.None) column.Field = match.Field;
        }
        Status = "Przypisano kolumny o typowych nagłówkach. Sprawdź i uzupełnij resztę.";
    }

    private static BomField Synonym(string header) => header.ToUpperInvariant().Replace(" ", "").Replace("_", "") switch
    {
        "DESIGNATOR" or "DESIGNATORS" or "REFDES" or "REFERENCE" or "REFERENCES" => BomField.Designator,
        "QTY" or "QUANTITY" or "ILOŚĆ" => BomField.Quantity,
        "VALUE" or "COMMENT" or "NAME" or "VAL" => BomField.Value,
        "FOOTPRINT" or "PACKAGE" or "CASE" or "PCBFOOTPRINT" => BomField.Footprint,
        "DESCRIPTION" or "DESC" or "OPIS" => BomField.Description,
        "PARTNUMBER" or "MPN" or "MANUFACTURERPARTNUMBER" or "MFRPARTNUMBER" => BomField.PartNumber,
        _ => BomField.None
    };

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
            : "Podgląd gotowy. Kliknij „Dalej: Import P&P”.";
    }

    /// <summary>Dalej: BOM zostaje zapisany w tle (bez osobnego kroku „rewizja”) i otwiera się Import P&amp;P.</summary>
    private async Task SaveAsync()
    {
        if (SelectedCustomer is null) { Status = "Wybierz Klienta."; return; }
        if (SelectedPackage is not { } package) { Status = "Wybierz pakiet (albo utwórz nowy)."; return; }
        if (_result is null) { Status = "Najpierw kliknij Podgląd."; return; }
        if (_result.HasErrors) { Status = "Nie można przejść dalej — podgląd zawiera błędy blokujące."; return; }
        if (_fileContent is null) { Status = "Najpierw wybierz plik BOM."; return; }
        IsBusy = true;
        try
        {
            var saved = await _service.SaveBomRevisionAsync(package.Id, _result, FilePath, SelectedSheet?.Name, Comment, _fileContent);
            Status = $"BOM wczytany do pakietu {package.DisplayName}: {saved.LineCount} linii.";
            Comment = "";
            if (_continueToPnp is not null) await _continueToPnp(package, saved.Id, CompareChoice);
        }
        catch (Exception e) { Status = ErrorMessages.Describe(e); }
        finally { IsBusy = false; }
    }

    /// <summary>Pola zaznaczone w kolumnie „Comparator” (BOM). Ostatecznie porównujemy pola zaznaczone i w BOM, i w P&amp;P.</summary>
    public ComparatorOptions CompareChoice => new(
        Columns.Any(c => c.Field == BomField.Value && c.Compare),
        Columns.Any(c => c.Field == BomField.Footprint && c.Compare),
        Columns.Any(c => c.Field == BomField.Description && c.Compare),
        Columns.Any(c => c.Field == BomField.PartNumber && c.Compare));

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
