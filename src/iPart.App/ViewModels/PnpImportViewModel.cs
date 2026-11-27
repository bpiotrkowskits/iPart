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

public sealed partial class PnpColumnRow : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public int Index { get; init; }
    public string Header { get; init; } = "";
    public string Sample { get; init; } = "";
    [ObservableProperty] public partial PnpField Field { get; set; }
}

/// <summary>
/// Import P&amp;P Klienta do rewizji importu (comparator): plik .txt albo .xlsx, wiersz nagłówków, mapowanie kolumn
/// (podpowiedź po nazwach nagłówków, do sprawdzenia), podgląd i zapis przez handler komendy.
/// </summary>
public sealed partial class PnpImportViewModel(PackageBomViewModel bom, ICommandHandler<AttachPnp> attach, Action back)
    : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private IReadOnlyList<(int RowNumber, IReadOnlyList<string?> Values)> _rows = [];
    private byte[]? _content;
    private PnpImportResult? _result;

    public string Title => $"Import P&P — {bom.Package.DisplayName}, {bom.SelectedRevision?.DisplayName}";
    public ObservableCollection<PnpColumnRow> Columns { get; } = [];
    public ObservableCollection<PnpRow> Preview { get; } = [];
    public IReadOnlyList<PnpFieldOption> FieldOptions { get; } = Enum.GetValues<PnpField>().Select(f => new PnpFieldOption(f, PnpFields.Label(f))).ToList();

    [ObservableProperty] public partial string FilePath { get; set; } = "";
    [ObservableProperty] public partial int HeaderRow { get; set; } = 1;
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
        _result = PnpImportBuilder.Build(PnpTextReader.ToTable(_rows, HeaderRow), Columns.Select(c => new PnpColumnAssignment(c.Index, c.Field)).ToList());
        foreach (var row in _result.Rows.Take(200)) Preview.Add(row);
        var errors = _result.Issues.Where(i => i.Severity == Core.Import.ImportSeverity.Error).Select(i => i.Message).ToList();
        Ready = !_result.HasErrors;
        Status = Ready ? $"Wierszy P&P: {_result.Rows.Count}. Sprawdź podgląd i kliknij „Zapisz P&P”." : string.Join(" ", errors.Take(5));
    }

    [RelayCommand(CanExecute = nameof(Ready))]
    private async Task SaveAsync()
    {
        if (_result is null || _content is null || bom.SelectedRevision is not { } revision) return;
        var result = await attach.HandleAsync(new AttachPnp(revision.Id, FilePath, _content, _result.Rows));
        if (!result.IsSuccess) { Status = result.Error ?? "Nie udało się zapisać P&P."; return; }
        await bom.ReloadAsync(null);
        bom.Status = $"Zapisano P&P ({_result.Rows.Count} wierszy). Sprawdź porównanie w kolumnie „Porównanie z P&P”.";
        back();
    }

    [RelayCommand] private void Back() => back();

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
        var h = (header ?? "").Trim().ToUpperInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
        return h switch
        {
            "DESIGNATOR" or "REFDES" or "REF" or "REFERENCE" or "PART" or "DESIGNATORS" or "OZNACZENIE" => PnpField.Designator,
            "X" or "MIDX" or "CENTERX" or "POSX" or "XMM" or "X(MM)" => PnpField.X,
            "Y" or "MIDY" or "CENTERY" or "POSY" or "YMM" or "Y(MM)" => PnpField.Y,
            "ROTATION" or "ROT" or "ANGLE" or "OBRÓT" => PnpField.Rotation,
            "LAYER" or "SIDE" or "TB" or "STRONA" => PnpField.Side,
            "FOOTPRINT" or "PATTERN" or "PACKAGE" or "PCBFOOTPRINT" => PnpField.Footprint,
            "VALUE" or "COMMENT" or "VAL" => PnpField.Value,
            _ => PnpField.None
        };
    }

    private static string Decode(byte[] content)
    {
        using var reader = new StreamReader(new MemoryStream(content), new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
