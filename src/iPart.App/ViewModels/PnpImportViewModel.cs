using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iPart.Core.Packages;
using iPart.Infrastructure.Packages;
using Microsoft.Win32;

namespace iPart.App.ViewModels;

public sealed record PnpFieldOption(PnpField Field, string Label);
public sealed record CoordinateUnitOption(CoordinateUnit Unit, string Label);

public sealed partial class PnpColumnRow : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public int Index { get; init; }
    public string Header { get; init; } = "";
    public string Sample { get; init; } = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanCompare))] public partial PnpField Field { get; set; }
    /// <summary>Kolumna „Comparator”: porównać tę kolumnę z BOM (tylko Value, Footprint, Description, PartNumber).</summary>
    public bool CanCompare => Field is PnpField.Value or PnpField.Footprint or PnpField.Description or PnpField.PartNumber;
    [ObservableProperty] public partial bool Compare { get; set; }
    partial void OnFieldChanged(PnpField value) => Compare = CanCompare && value != PnpField.PartNumber;
}

/// <summary>
/// Import P&amp;P Klienta do rewizji importu (comparator): plik .txt albo .xlsx, wiersz nagłówków, mapowanie kolumn
/// (podpowiedź po nazwach nagłówków, do sprawdzenia), podgląd i zapis przez handler komendy.
/// </summary>
/// <param name="back">Przejście do Comparatora (po zapisie albo „Pomiń P&amp;P”).</param>
/// <param name="backToBom">Powrót do kroku 1 (Import BOM) — wczytany BOM jest usuwany.</param>
public sealed partial class PnpImportViewModel(PackageBomViewModel bom, ICommandHandler<AttachPnp> attach, Action back,
    Func<Task>? backToBom = null, Func<string, bool>? confirm = null)
    : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private IReadOnlyList<(int RowNumber, IReadOnlyList<string?> Values)> _rows = [];
    private byte[]? _content;
    private PnpImportResult? _result;

    public string Title => $"Import P&P  ·  {bom.Package.DisplayName}";
    public bool CanGoBackToBom => backToBom is not null;
    public ObservableCollection<PnpColumnRow> Columns { get; } = [];
    public ObservableCollection<PnpRow> Preview { get; } = [];
    public IReadOnlyList<PnpFieldOption> FieldOptions { get; } = Enum.GetValues<PnpField>().Select(f => new PnpFieldOption(f, PnpFields.Label(f))).ToList();

    [ObservableProperty] public partial string FilePath { get; set; } = "";
    [ObservableProperty] public partial int HeaderRow { get; set; } = 1;
    /// <summary>Jednostka współrzędnych X, Y w pliku; „Auto” — z nagłówka kolumny albo z wartości (bez jednostki = mm).</summary>
    [ObservableProperty] public partial CoordinateUnitOption? Unit { get; set; }
    public IReadOnlyList<CoordinateUnitOption> Units { get; } =
        [new(CoordinateUnit.Auto, "Automatycznie (z nagłówka / wartości)"), new(CoordinateUnit.Millimeters, "mm"), new(CoordinateUnit.Mils, "mils (1 mil = 0,0254 mm)")];
    [ObservableProperty] public partial string Status { get; set; } = "Wybierz plik P&P Klienta (.txt albo .xlsx).";
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))] public partial bool Ready { get; set; }

    partial void OnHeaderRowChanged(int value) => LoadColumns();

    [RelayCommand]
    private async Task ChooseFileAsync()
    {
        var dialog = new OpenFileDialog { Filter = "P&P (*.txt;*.csv;*.pos;*.xlsx)|*.txt;*.csv;*.pos;*.xlsx|Wszystkie pliki|*.*", CheckFileExists = true, Title = "Wybierz P&P Klienta" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var content = await File.ReadAllBytesAsync(dialog.FileName);
            _rows = Path.GetExtension(dialog.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
                ? BomWorkbookReader.Read(content).FirstOrDefault(s => s.Rows.Count > 0)?.Rows ?? []
                : PnpTextReader.Read(Decode(content));
            _content = content;
            Unit ??= Units[0];
            FilePath = dialog.FileName;
            // Nagłówek: pierwszy wiersz, który zawiera kolumnę wyglądającą jak desygnator.
            HeaderRow = _rows.FirstOrDefault(r => r.Values.Any(v => Suggest(v) == PnpField.Designator)).RowNumber is var h and > 0 ? h : _rows.FirstOrDefault().RowNumber;
            LoadColumns();
        }
        catch (Exception e) { Status = ErrorMessages.Describe(e); }
    }

    [RelayCommand]
    private void ShowPreview()
    {
        Ready = false;
        Preview.Clear();
        if (_rows.Count == 0) { Status = "Najpierw wybierz plik."; return; }
        var unit = Unit?.Unit ?? CoordinateUnit.Auto;
        if (unit == CoordinateUnit.Auto)
            unit = Columns.Where(c => c.Field is PnpField.X or PnpField.Y).Select(c => Coordinates.FromHeader(c.Header)).FirstOrDefault(u => u != CoordinateUnit.Auto);
        _result = PnpImportBuilder.Build(PnpTextReader.ToTable(_rows, HeaderRow), Columns.Select(c => new PnpColumnAssignment(c.Index, c.Field)).ToList(), unit);
        foreach (var row in _result.Rows.Take(200)) Preview.Add(row);
        var errors = _result.Issues.Where(i => i.Severity == Core.Import.ImportSeverity.Error).Select(i => i.Message).ToList();
        Ready = !_result.HasErrors;
        var warnings = _result.Issues.Where(i => i.Severity == Core.Import.ImportSeverity.Warning).Select(i => i.Message);
        Status = Ready ? $"Wierszy P&P: {_result.Rows.Count}. Współrzędne w {(unit == CoordinateUnit.Mils ? "mils → przeliczone na mm" : "mm")}. {string.Join(" ", warnings)} Sprawdź podgląd i kliknij „Dalej: Comparator”." : string.Join(" ", errors.Take(5));
    }

    [RelayCommand(CanExecute = nameof(Ready))]
    private async Task SaveAsync()
    {
        if (_result is null || _content is null || bom.SelectedRevision is not { } revision) return;
        // Porównujemy pola zaznaczone w kolumnie „Comparator” i przy BOM, i przy P&P.
        var bomChoice = bom.Options;
        var options = new ComparatorOptions(bomChoice.Value && Chosen(PnpField.Value), bomChoice.Footprint && Chosen(PnpField.Footprint),
            bomChoice.Description && Chosen(PnpField.Description), bomChoice.PartNumber && Chosen(PnpField.PartNumber));
        var result = await attach.HandleAsync(new AttachPnp(revision.Id, FilePath, _content, _result.Rows, options));
        if (!result.IsSuccess) { Status = result.Error ?? "Nie udało się zapisać P&P."; return; }
        bom.Stage = PackageBomViewModel.ComparatorStage;
        await bom.ReloadAsync();
        bom.Status = $"Wczytano P&P ({_result.Rows.Count} wierszy). Porównywane: {options.Describe()}. Linie zgodne są zatwierdzone automatycznie — sprawdź niezgodności.";
        back();
    }

    private bool Chosen(PnpField field) => Columns.Any(c => c.Field == field && c.Compare);

    /// <summary>Pomiń P&amp;P — Comparator bez P&amp;P.</summary>
    [RelayCommand] private void Back() { bom.Stage = PackageBomViewModel.ComparatorStage; back(); }

    /// <summary>Wróć do kroku 1: po potwierdzeniu wczytany BOM jest usuwany.</summary>
    [RelayCommand]
    private async Task BackToBomAsync()
    {
        if (backToBom is null) return;
        if (confirm is not null && !confirm("Wszystkie wprowadzone zmiany zostaną utracone. Czy wrócić do kroku 1 (Import BOM)?")) return;
        await backToBom();
    }

    private void LoadColumns()
    {
        Ready = false;
        Columns.Clear();
        if (_rows.Count == 0) return;
        var table = PnpTextReader.ToTable(_rows, HeaderRow);
        var used = new HashSet<PnpField>();
        for (var i = 0; i < table.Headers.Count; i++)
        {
            var index = i;
            var suggestion = Suggest(table.Headers[i]);
            if (suggestion != PnpField.None && !used.Add(suggestion)) suggestion = PnpField.None;
            var samples = table.Rows.Take(3).Select(r => index < r.Values.Count ? r.Values[index] : null).Where(v => !string.IsNullOrWhiteSpace(v));
            Columns.Add(new PnpColumnRow { Index = i, Header = table.Headers[i], Sample = string.Join(" | ", samples), Field = suggestion });
        }
        Status = "Sprawdź przypisanie kolumn (podpowiedź po nazwach nagłówków) i kliknij „Podgląd”.";
    }

    /// <summary>Podpowiedź pola po nazwie nagłówka — tylko propozycja, decyduje użytkownik.</summary>
    private static PnpField Suggest(string? header)
    {
        // Bez spacji, myślników i jednostki w nawiasie: „Center-X(mil)” → „CENTERX”.
        var h = System.Text.RegularExpressions.Regex.Replace((header ?? "").Trim().ToUpperInvariant(), @"\(.*?\)|\[.*?\]", "").Replace(" ", "").Replace("_", "").Replace("-", "");
        return h switch
        {
            "DESIGNATOR" or "REFDES" or "REF" or "REFERENCE" or "PART" or "DESIGNATORS" or "OZNACZENIE" => PnpField.Designator,
            "X" or "MIDX" or "CENTERX" or "POSX" or "XMM" or "XMIL" or "REFX" or "LOCATIONX" or "POSITIONX" => PnpField.X,
            "Y" or "MIDY" or "CENTERY" or "POSY" or "YMM" or "YMIL" or "REFY" or "LOCATIONY" or "POSITIONY" => PnpField.Y,
            "ROTATION" or "ROT" or "ANGLE" or "OBRÓT" => PnpField.Rotation,
            "LAYER" or "SIDE" or "TB" or "STRONA" => PnpField.Side,
            "FOOTPRINT" or "PATTERN" or "PACKAGE" or "PCBFOOTPRINT" => PnpField.Footprint,
            "VALUE" or "COMMENT" or "VAL" => PnpField.Value,
            "DESCRIPTION" or "DESC" or "OPIS" => PnpField.Description,
            "PARTNUMBER" or "MPN" or "PN" or "MANUFACTURERPARTNUMBER" or "MFRPARTNUMBER" => PnpField.PartNumber,
            _ => PnpField.None
        };
    }

    private static string Decode(byte[] content)
    {
        using var reader = new StreamReader(new MemoryStream(content), new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
