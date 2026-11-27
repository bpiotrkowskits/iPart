using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iPart.Core.Components;
using iPart.Core.Packages;
using ProduMan.SharedKernel.Common;

namespace iPart.App.ViewModels;

/// <summary>Handlery komend procesu BOM — ViewModel wywołuje je przez interfejsy, nigdy bazę (ADR-0014).</summary>
public sealed record BomHandlers(
    ICommandHandler<ResolveLine> Resolve,
    ICommandHandler<SetComparisonDecision> Decide,
    ICommandHandler<RecordCustomerAnswer> Answer,
    ICommandHandler<UpdateLineTs> UpdateTs,
    ICommandHandler<AddLineMpn> AddMpn,
    ICommandHandler<RemoveLineMpn> RemoveMpn,
    ICommandHandler<ApprovePnp> ApprovePnp,
    ICommandHandler<ApproveBom> ApproveBom,
    ICommandHandler<AttachPnp> AttachPnp,
    ICommandHandler<SetComparatorOptions>? SetOptions = null,
    ICommandHandler<SplitLine>? Split = null,
    ICommandHandler<ResetComparator>? Reset = null);

/// <summary>Operacje na plikach dla ekranu (okna wyboru pliku dostarcza aplikacja — ViewModel nie zna WPF).</summary>
public sealed record FileDialogs(Func<string, byte[], bool> SaveExcel, Func<byte[]?> OpenExcel);

/// <summary>Kolumna Klienta do podglądu w iParcie: nagłówek i wartość z oryginalnego BOM.</summary>
public sealed record CustomerCell(string Header, string? Value);

/// <summary>
/// Linia BOM do wyświetlenia: Comparator (BOM i P&amp;P obok siebie, różnice wg wybranych pól), Wykaz Różnic,
/// iPart roboczy (kolumny rdzenia BOM), dane Klienta i podpowiedź z pełną treścią (komórki skracane do jednej linii).
/// </summary>
public sealed class BomLineItem
{
    private readonly BomLine _line;

    public BomLineItem(BomLine line, LineComparison? comparison = null, IReadOnlyList<LineMpn>? mpns = null, IReadOnlyList<PnpRow>? pnp = null,
        ComparatorOptions? options = null, IReadOnlyDictionary<string, ComponentCore>? components = null)
    {
        _line = line;
        pnp ??= [];
        HasPnp = pnp.Count > 0;
        Groups = comparison?.Groups ?? [];
        Differences = comparison?.Differences ?? [];
        Approved = (mpns ?? []).Where(m => !m.Forbidden).OrderBy(m => m.Ordinal).ToList();
        Forbidden = (mpns ?? []).Where(m => m.Forbidden).ToList();
        Fields = LineFields.For(line, pnp, options);
        Wrd = DifferenceListRow.For(line, pnp);
        CanSplit = !line.Confirmed && HasPnp && LineFields.SplitGroups(line, pnp, options).Count > 1;
        DesignatorsPnpList = string.Join(", ", DesignatorRanges.Sort(LineFields.RowsOf(line, pnp).Select(p => p.Designator)));
        Core = LineCore.For(line, mpns ?? [], components ?? new Dictionary<string, ComponentCore>());
        CustomerData =
        [
            new("Designator", line.Designators), new("Quantity", line.Quantity?.ToString()), new("Value", line.Value),
            new("Footprint", line.Footprint), new("Description", line.Description), new("PartNumber", line.PartNumber), new("Notes", line.Notes),
            .. line.ExtraValues.OrderBy(x => x.Ordinal).Select(x => new CustomerCell(x.Header ?? $"Kolumna {x.Ordinal}", x.Value))
        ];
        Extras = string.Join("; ", line.ExtraValues.OrderBy(x => x.Ordinal).Select(x => $"{x.Header}: {x.Value}"));
    }

    public BomLine Line => _line;
    public Guid Id => _line.Id;
    public int LineNumber => _line.LineNumber;
    public string LineLabel => _line.SplitFromLineNumber is { } from ? $"{LineNumber} (z {from})" : LineNumber.ToString();
    public string? Designators => _line.Designators;
    public int DesignatorCount => _line.ExpandedDesignators.Count;
    public int? Quantity => _line.Quantity;
    public string? PartTs => _line.PartTs;
    public string? MountingCode => _line.MountingCode;
    public string? Footprint => _line.Footprint;
    public string? Description => _line.Description;
    public string? Value => _line.Value;
    public string? PartNumber => _line.PartNumber;
    public string? Notes => _line.Notes;
    public string? TsFootprint => _line.TsFootprint;
    public string? TsDescription => _line.TsDescription;
    public string? TsValue => _line.TsValue;
    public bool Confirmed => _line.Confirmed;
    public string? Question => _line.DifferenceQuestion;
    public string? CustomerAnswer => _line.CustomerAnswer;
    public bool InDifferenceList => _line.ComparisonDecision == ComparisonDecisions.DifferenceList;
    /// <summary>Linia czeka w Wykazie Różnic — w Comparatorze nie można jej zatwierdzić.</summary>
    public bool WaitsInDifferenceList => InDifferenceList && !Confirmed;
    public bool CanDecideInComparator => !Confirmed && !InDifferenceList;
    public bool HasPnp { get; }
    public string Extras { get; }
    public IReadOnlyList<CustomerCell> CustomerData { get; }
    /// <summary>Grupy desygnatorów o tym samym wyniku porównania (podlinie), zapisane zakresami.</summary>
    public IReadOnlyList<DesignatorGroup> Groups { get; }
    public IReadOnlyList<LineDifference> Differences { get; }
    public IReadOnlyList<LineMpn> Approved { get; }
    public IReadOnlyList<LineMpn> Forbidden { get; }
    /// <summary>Pola porównywane z P&amp;P (BOM i P&amp;P) — do wyboru wartości przy zatwierdzaniu.</summary>
    public IReadOnlyList<FieldValues> Fields { get; }
    /// <summary>Wiersz Wykazu Różnic w układzie firmowego wzoru.</summary>
    public DifferenceListRow Wrd { get; }
    /// <summary>Desygnatory linii mają w P&amp;P różne dane — linię można (a przy pustym Footprint w BOM trzeba) rozdzielić.</summary>
    public bool CanSplit { get; }
    /// <summary>iPart roboczy: Case, Footprint, Value (z bazy albo z danych Klienta), PartNumber.</summary>
    public LineCore Core { get; }

    // ── Comparator: 8 kolumn jak w dotychczasowym comparatorze ──
    private FieldValues Field(string name) => Fields.Single(f => f.Field == name);
    public string DesignatorsPnp => Wrd.DesignatorsPnp;
    /// <summary>Desygnatory P&P wylistowane po przecinku (bez zakresów) — kolumna Comparatora.</summary>
    public string DesignatorsPnpList { get; }
    public string? ValueBom => Value;
    public string ValuePnp => Field(ComparedFields.Value).PnpText;
    public string? FootprintBom => Footprint ?? (TsFootprint is { } fromPnp ? $"(brak — z P&P: {fromPnp})" : null);
    public string FootprintPnp => Field(ComparedFields.Footprint).PnpText;
    public string? DescriptionBom => Description;
    public string DescriptionPnp => Field(ComparedFields.Description).PnpText;
    public string? PartNumberBom => PartNumber;
    public string PartNumberPnp => Field(ComparedFields.PartNumber).PnpText;
    public bool DesignatorsDiffer => HasPnp && Differences.Any(d => d.Kind == "Brak w P&P");
    public bool ValueDiffers => Field(ComparedFields.Value).Differs;
    public bool FootprintDiffers => Field(ComparedFields.Footprint).Differs || Field(ComparedFields.Footprint).NeedsSplit;
    public bool DescriptionDiffers => Field(ComparedFields.Description).Differs;
    public bool PartNumberDiffers => Field(ComparedFields.PartNumber).Differs;
    public bool HasDifferences => Differences.Count > 0;

    /// <summary>Czy komórka Comparatora (kolumna o tej ścieżce wiązania) zawiera wartość różną od P&P.</summary>
    public bool CellDiffers(string member) => member switch
    {
        nameof(Designators) or nameof(DesignatorsPnpList) => DesignatorsDiffer,
        nameof(ValueBom) or nameof(ValuePnp) => ValueDiffers,
        nameof(FootprintBom) or nameof(FootprintPnp) => FootprintDiffers,
        nameof(DescriptionBom) or nameof(DescriptionPnp) => DescriptionDiffers,
        nameof(PartNumberBom) or nameof(PartNumberPnp) => PartNumberDiffers,
        _ => false
    };

    public string MpnSummary => Forbidden.Count == 0 ? $"{Approved.Count} zatw." : $"{Approved.Count} zatw. · {Forbidden.Count} niedozw.";
    public string State => Confirmed ? "zatwierdzona" : InDifferenceList ? (CustomerAnswer is null ? "w Wykazie Różnic — czeka na Klienta" : "w Wykazie Różnic — jest odpowiedź") : "niezatwierdzona";
    public string? QuantityWarning => Differences.FirstOrDefault(d => d.Kind == "Ilość ≠ desygnatory")?.Detail;

    public string ComparisonText =>
        Groups.Count == 0 || Groups.All(g => g.Result == "brak P&P") ? "—"
        : Differences.Count == 0 ? "zgodne"
        : string.Join("; ", Differences.Select(d => d.Detail));

    /// <summary>Kolor wiersza; kolejność przy kilku znaczeniach: DNP → niezatwierdzona → montaż inny niż SMT (8.4).</summary>
    public string ColorKey => MountingCode == "DNP" ? "Dnp" : !Confirmed ? "Unconfirmed" : MountingCategories.IsNonSmt(MountingCode) ? "NonSmt" : "";

    public string ConfirmationText => !Confirmed ? "Linia niezatwierdzona."
        : $"Zatwierdzona — {ConfirmationBases.Describe(_line.ConfirmationBasis)}"
          + (string.IsNullOrWhiteSpace(_line.ConfirmationNote) ? "" : $": {_line.ConfirmationNote}")
          + $" ({_line.ConfirmedBy}, {_line.ConfirmedAtUtc?.ToLocalTime():dd.MM.yyyy HH:mm})";

    /// <summary>Pełna informacja o linii w chmurce po najechaniu — komórki tabeli są skracane do jednej linii.</summary>
    public string Tooltip => string.Join(Environment.NewLine, new[]
    {
        $"Linia {LineLabel} — {State}",
        $"Desygnatory BOM ({DesignatorCount}): {Designators}",
        $"Desygnatory P&P: {(DesignatorsPnp.Length == 0 ? "—" : DesignatorsPnp)}",
        $"Value — BOM: {Value ?? "—"}   P&P: {ValuePnp}",
        $"Footprint — BOM: {Footprint ?? "—"}   P&P: {FootprintPnp}",
        $"Description — BOM: {Description ?? "—"}   P&P: {DescriptionPnp}",
        $"PartNumber — BOM: {PartNumber ?? "—"}   P&P: {PartNumberPnp}",
        $"Porównanie z P&P: {ComparisonText}",
    }.Concat(Extras.Length > 0 ? [$"Inne kolumny: {Extras}"] : []));

    public bool Matches(string text) => text.Length == 0
        || (Designators?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)
        || (PartNumber?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)
        || (Description?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)
        || (Value?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)
        || (Footprint?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false);
}

/// <summary>Wybór wartości jednego pola przy zatwierdzaniu: z BOM, z P&amp;P albo własna.</summary>
public sealed partial class FieldChoice(FieldValues values) : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public string Field => values.Field;
    public string Label => values.Label;
    public string BomText => $"BOM: {values.Bom ?? "—"}";
    public string PnpText => $"P&P: {values.PnpText}";
    /// <summary>P&amp;P do wyboru tylko przy jednej wartości dla wszystkich desygnatorów linii.</summary>
    public bool PnpAvailable => values.PnpSingle is not null;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsBom), nameof(IsPnp), nameof(IsOwn))] public partial ValueSource Source { get; set; } = ValueSource.Bom;
    [ObservableProperty] public partial string Own { get; set; } = "";

    public bool IsBom { get => Source == ValueSource.Bom; set { if (value) Source = ValueSource.Bom; } }
    public bool IsPnp { get => Source == ValueSource.Pnp; set { if (value) Source = ValueSource.Pnp; } }
    public bool IsOwn { get => Source == ValueSource.Own; set { if (value) Source = ValueSource.Own; } }

    public FieldResolution ToResolution() => new(Field, Source, Own);
}

/// <summary>
/// Pakiet po imporcie: Comparator (porównanie BOM–P&amp;P i rozstrzygnięcia), Wykaz Różnic, Tylko w P&amp;P,
/// iPart roboczy (kolumny rdzenia BOM z bazy albo z danych Klienta) oraz zatwierdzenie i wersje.
/// Każda zakładka ma swój panel po prawej. Wszystkie zapisy przez handlery komend.
/// </summary>
public sealed partial class PackageBomViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public const int ComparatorStage = 3;
    public const int IntegrationStage = 4;
    public const int ApprovalStage = 5;

    private readonly IPackageService _packages;
    private readonly IBomWorkflowReader _reader;
    private readonly BomHandlers _handlers;
    private readonly IPackageHistoryReader _history;
    private readonly IDifferenceListWorkbook? _workbook;
    private readonly FileDialogs? _files;
    private readonly IComponentLookup? _components;
    private readonly Action _back;
    private readonly Action<PackageBomViewModel>? _openPnpImport;
    private IReadOnlyList<BomLineItem> _all = [];
    private bool _loadingOptions;
    private readonly Func<string, bool> _confirm;

    public PackageBomViewModel(PackageSummary package, IPackageService packages, IBomWorkflowReader reader, BomHandlers handlers,
        IPackageHistoryReader history, Action back, Action<PackageBomViewModel>? openPnpImport = null,
        IDifferenceListWorkbook? workbook = null, FileDialogs? files = null, IComponentLookup? components = null, Func<string, bool>? confirm = null)
    {
        Package = package;
        _packages = packages;
        _reader = reader;
        _handlers = handlers;
        _history = history;
        _back = back;
        _openPnpImport = openPnpImport;
        _workbook = workbook;
        _files = files;
        _components = components;
        _confirm = confirm ?? (_ => true);
    }

    public PackageSummary Package { get; }
    public ObservableCollection<BomRevisionSummary> Revisions { get; } = [];
    public ObservableCollection<BomLineItem> Lines { get; } = [];
    public ObservableCollection<BomLineItem> CoreLines { get; } = [];
    public ObservableCollection<BomLineItem> DifferenceList { get; } = [];
    public ObservableCollection<PnpRow> OnlyInPnpRows { get; } = [];
    public ObservableCollection<HistoryEntry> History { get; } = [];
    public ObservableCollection<BomVersion> Versions { get; } = [];
    public ObservableCollection<FieldChoice> LineChoices { get; } = [];
    public ObservableCollection<FieldChoice> WrdChoices { get; } = [];

    /// <summary>Krok procesu na tym ekranie: 3 — Comparator (z Wykazem Różnic i Tylko w P&P), 4 — Integracja Danych.</summary>
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsComparatorStage), nameof(IsIntegrationStage), nameof(IsApprovalStage))] public partial int Stage { get; set; } = ComparatorStage;
    public bool IsApprovalStage => Stage == ApprovalStage;
    public bool IsComparatorStage => Stage == ComparatorStage;
    public bool IsIntegrationStage => Stage == IntegrationStage;
    /// <summary>Zakładka w kroku 3: 0 Comparator, 1 Wykaz Różnic, 2 Tylko w P&P.</summary>
    [ObservableProperty] public partial int ComparatorTab { get; set; }
    /// <summary>Zakładka w kroku 4: 0 Integracja Danych, 1 Zatwierdzenie i wersje.</summary>
    [ObservableProperty] public partial int IntegrationTab { get; set; }
    [ObservableProperty] public partial BomRevisionSummary? SelectedRevision { get; set; }
    [ObservableProperty] public partial string Search { get; set; } = "";
    [ObservableProperty] public partial string Status { get; set; } = "";
    [ObservableProperty] public partial string Summary { get; set; } = "";
    [ObservableProperty] public partial string PnpState { get; set; } = "";
    [ObservableProperty] public partial string OnlyInPnpText { get; set; } = "";
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ApprovePnpCommand))] public partial bool CanApprovePnpNow { get; set; }

    // ── Comparator: porównywane pola i filtr ──
    [ObservableProperty] public partial bool CompareValue { get; set; } = true;
    [ObservableProperty] public partial bool CompareFootprint { get; set; } = true;
    [ObservableProperty] public partial bool CompareDescription { get; set; } = true;
    [ObservableProperty] public partial bool ComparePartNumber { get; set; }
    [ObservableProperty] public partial bool OnlyDifferences { get; set; }

    // ── Comparator: panel linii ──
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApproveLineCommand), nameof(AddToDifferenceListCommand), nameof(SplitLineCommand))]
    public partial BomLineItem? SelectedLine { get; set; }
    /// <summary>Decyzja dla linii: najpierw wybór — zatwierdzić czy do Wykazu Różnic.</summary>
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsDifferenceListAction))] public partial bool IsApproveAction { get; set; } = true;
    public bool IsDifferenceListAction { get => !IsApproveAction; set => IsApproveAction = !value; }
    [ObservableProperty] public partial string Question { get; set; } = "";
    [ObservableProperty] public partial string LineComment { get; set; } = "";

    // ── Wykaz Różnic: panel pozycji ──
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ApproveWrdCommand))] public partial BomLineItem? SelectedWrd { get; set; }
    /// <summary>Podstawa zatwierdzenia z Wykazu Różnic: odpowiedź Klienta albo wcześniejsza realizacja.</summary>
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsPreviousRealization))] public partial bool IsCustomerAnswer { get; set; } = true;
    public bool IsPreviousRealization { get => !IsCustomerAnswer; set => IsCustomerAnswer = !value; }
    [ObservableProperty] public partial string WrdText { get; set; } = "";

    // ── iPart roboczy: dane Klienta pozycji pod kursorem (albo zaznaczonej) ──
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(PreviewLine))] public partial BomLineItem? SelectedCoreLine { get; set; }
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(PreviewLine))] public partial BomLineItem? HoveredCoreLine { get; set; }
    public BomLineItem? PreviewLine => HoveredCoreLine ?? SelectedCoreLine;

    // ── Zatwierdzenie i wersje ──
    [ObservableProperty] public partial bool ApproveForOrder { get; set; } = true;
    [ObservableProperty] public partial string OrderNumber { get; set; } = "";
    [ObservableProperty] public partial string ApproveComment { get; set; } = "";
    [ObservableProperty] public partial string ApproveConditions { get; set; } = "";

    public ComparatorOptions Options => new(CompareValue, CompareFootprint, CompareDescription, ComparePartNumber);

    partial void OnSelectedRevisionChanged(BomRevisionSummary? value) => _ = ReloadAsync();
    partial void OnSearchChanged(string value) => ApplyFilter();
    partial void OnOnlyDifferencesChanged(bool value) => ApplyFilter();
    partial void OnCompareValueChanged(bool value) => _ = SaveOptionsAsync();
    partial void OnCompareFootprintChanged(bool value) => _ = SaveOptionsAsync();
    partial void OnCompareDescriptionChanged(bool value) => _ = SaveOptionsAsync();
    partial void OnComparePartNumberChanged(bool value) => _ = SaveOptionsAsync();

    partial void OnSelectedLineChanged(BomLineItem? value)
    {
        IsApproveAction = true;
        Question = value?.Question ?? "";
        LineComment = "";
        Fill(LineChoices, value);
    }

    partial void OnSelectedWrdChanged(BomLineItem? value)
    {
        IsCustomerAnswer = true;
        WrdText = value?.CustomerAnswer ?? "";
        Fill(WrdChoices, value);
    }

    private static void Fill(ObservableCollection<FieldChoice> target, BomLineItem? line)
    {
        target.Clear();
        foreach (var f in line?.Fields.Where(f => f.Differs) ?? []) target.Add(new FieldChoice(f));
    }

    public async Task LoadAsync(Guid? revisionId = null)
    {
        try
        {
            Revisions.Clear();
            foreach (var r in await _packages.ListRevisionsAsync(Package.Id)) Revisions.Add(r);
            if (Revisions.Count == 0) Status = "Pakiet nie ma jeszcze BOM — wczytaj go w „Import BOM/P&P”.";
            var target = Revisions.FirstOrDefault(r => r.Id == revisionId) ?? Revisions.FirstOrDefault();
            if (target == SelectedRevision) await ReloadAsync(); else SelectedRevision = target;
        }
        catch (Exception e) { Status = ErrorMessages.Describe(e); }
    }

    /// <summary>Ponowne wczytanie linii, P&amp;P, MPN, wersji i historii — po każdej zapisanej komendzie.</summary>
    public async Task ReloadAsync()
    {
        try
        {
            var selectLine = SelectedLine?.Id;
            var selectWrd = SelectedWrd?.Id;
            var selectCore = SelectedCoreLine?.Id;
            if (SelectedRevision is not { } revision) { _all = []; ApplyFilter(); return; }
            var full = await _reader.RevisionAsync(revision.Id);
            _loadingOptions = true;
            var options = ComparatorOptions.Parse(full?.CompareFields);
            (CompareValue, CompareFootprint, CompareDescription, ComparePartNumber) = (options.Value, options.Footprint, options.Description, options.PartNumber);
            _loadingOptions = false;

            var lines = await _packages.GetLinesAsync(revision.Id);
            var pnp = await _reader.PnpRowsAsync(revision.Id);
            var mpns = await _reader.MpnsAsync(revision.Id);
            var components = _components is null ? null : await _components.FindAsync(mpns.Where(m => !m.Forbidden).Select(m => m.Mpn).ToList());
            var comparison = BomPnpComparer.Compare(lines, pnp, options);
            _all = lines.OrderBy(l => l.LineNumber).Select(l => new BomLineItem(l, comparison.Lines.FirstOrDefault(c => c.LineId == l.Id),
                mpns.Where(m => m.BomLineId == l.Id).ToList(), pnp, options, components)).ToList();
            ApplyFilter();
            SelectedLine = Lines.FirstOrDefault(l => l.Id == selectLine);

            DifferenceList.Clear();
            foreach (var l in _all.Where(l => l.InDifferenceList)) DifferenceList.Add(l);
            SelectedWrd = DifferenceList.FirstOrDefault(l => l.Id == selectWrd);
            CoreLines.Clear();
            foreach (var l in _all) CoreLines.Add(l);
            SelectedCoreLine = CoreLines.FirstOrDefault(l => l.Id == selectCore);
            HoveredCoreLine = null;

            var onlyInPnp = comparison.OnlyInPnp.Select(BomPnpComparer.Normalize).ToHashSet();
            OnlyInPnpRows.Clear();
            foreach (var row in pnp.Where(p => onlyInPnp.Contains(BomPnpComparer.Normalize(p.Designator)))
                         .OrderBy(p => DesignatorRanges.Sort(comparison.OnlyInPnp).ToList().IndexOf(p.Designator)))
                OnlyInPnpRows.Add(row);
            OnlyInPnpText = comparison.OnlyInPnp.Count == 0 ? "Brak — wszystkie desygnatory P&P są w BOM." : $"{comparison.OnlyInPnp.Count} szt.: {comparison.OnlyInPnpText}";

            var unconfirmed = _all.Count(l => !l.Confirmed);
            Summary = $"{_all.Count} linii · niezatwierdzone: {unconfirmed} · niezgodności z P&P: {comparison.Differences} · w Wykazie Różnic: {DifferenceList.Count}";
            PnpState = full?.PnpFileName is null ? "P&P: brak — zaimportuj plik P&P"
                : full.PnpApprovedAtUtc is null ? $"P&P: {full.PnpFileName} ({pnp.Count} wierszy) — czeka na zatwierdzenie przez Technologa SMT"
                : $"P&P: {full.PnpFileName} — zatwierdzone ({full.PnpApprovedBy}, {full.PnpApprovedAtUtc?.ToLocalTime():dd.MM.yyyy HH:mm})";
            CanApprovePnpNow = full?.PnpFileName is not null && full.PnpApprovedAtUtc is null;
            ApproveConditions = string.Join(Environment.NewLine, new[]
            {
                unconfirmed == 0 ? "✓ Wszystkie linie zatwierdzone." : $"! {unconfirmed} linii niezatwierdzonych — wejdą do wersji, ale zakupy ich nie liczą, dopóki ich nie zatwierdzisz.",
                DifferenceList.Any(l => !l.Confirmed) ? $"! {DifferenceList.Count(l => !l.Confirmed)} pozycji Wykazu Różnic bez zatwierdzenia." : "✓ Wykaz Różnic rozstrzygnięty.",
                full?.PnpApprovedAtUtc is not null ? "✓ P&P zatwierdzone." : "! P&P niezatwierdzone — nie blokuje BOM, blokuje gotowość dla produkcji SMT.",
            });

            Versions.Clear();
            foreach (var v in await _reader.VersionsAsync(Package.Id)) Versions.Add(v);
            History.Clear();
            foreach (var entry in await _history.ListAsync(Package.Id)) History.Add(entry);
        }
        catch (Exception e) { _loadingOptions = false; Status = ErrorMessages.Describe(e); }
    }

    [RelayCommand] private void Back() => _back();

    // ── Przejścia między krokami ──

    [RelayCommand] private void NextStage() => Stage = Stage == ComparatorStage ? IntegrationStage : ApprovalStage;
    [RelayCommand] private void BackToIntegration() => Stage = IntegrationStage;
    [RelayCommand] private void BackToComparator() => Stage = ComparatorStage;

    /// <summary>Wróć do kroku 2 (Import P&amp;P): po potwierdzeniu P&amp;P i wszystkie rozstrzygnięcia Comparatora są cofane.</summary>
    [RelayCommand]
    private async Task BackToPnpAsync()
    {
        if (SelectedRevision is not { } revision || _openPnpImport is null) return;
        if (!_confirm("Wszystkie wprowadzone zmiany zostaną utracone. Czy wrócić do kroku 2 (Import P&P)?")) return;
        if (_handlers.Reset is { } reset)
        {
            var result = await reset.HandleAsync(new ResetComparator(revision.Id));
            if (!result.IsSuccess) { Status = result.Error ?? "Nie udało się wrócić do kroku 2."; return; }
        }
        await ReloadAsync();
        _openPnpImport(this);
    }

    // ── Comparator ──

    private async Task SaveOptionsAsync()
    {
        if (_loadingOptions || SelectedRevision is not { } revision) return;
        if (_handlers.SetOptions is null) { ApplyFilter(); return; }
        await Run(null, _ => _handlers.SetOptions.HandleAsync(new SetComparatorOptions(revision.Id, Options)), $"Porównywane pola: {Options.Describe()}");
    }

    /// <summary>Zatwierdź linię: wartości różniących się pól z BOM albo z P&amp;P (decyzja Opiekuna).</summary>
    [RelayCommand(CanExecute = nameof(CanDecide))]
    private Task ApproveLineAsync() => Run(SelectedLine, line => _handlers.Resolve.HandleAsync(
        new ResolveLine(line.Id, ConfirmationBases.CoordinatorDecision, LineComment, LineChoices.Select(c => c.ToResolution()).ToList())), "Zatwierdzono");

    [RelayCommand(CanExecute = nameof(CanDecide))]
    private Task AddToDifferenceListAsync() => Run(SelectedLine, line => _handlers.Decide.HandleAsync(
        new SetComparisonDecision(line.Id, ComparisonDecisions.DifferenceList, Question)), "Dodano do Wykazu Różnic");
    // Linia w Wykazie Różnic: zatwierdzenie tylko w zakładce Wykaz Różnic (albo później w edycji zatwierdzonego iPartu).
    private bool CanDecide() => SelectedLine is { Confirmed: false, InDifferenceList: false };

    /// <summary>Rozdziel linię według grup P&amp;P (np. R1–R5 0402 i R6–R10 0603) — nowe linie na końcu.</summary>
    [RelayCommand(CanExecute = nameof(CanSplit))]
    private Task SplitLineAsync() => _handlers.Split is null ? Task.CompletedTask
        : Run(SelectedLine, line => _handlers.Split.HandleAsync(new SplitLine(line.Id)), "Rozdzielono linię");
    private bool CanSplit() => SelectedLine is { CanSplit: true };

    // ── Wykaz Różnic ──

    /// <summary>Zatwierdź pozycję Wykazu: na podstawie odpowiedzi Klienta albo wcześniejszej realizacji; wartości z BOM, P&amp;P albo własne.</summary>
    [RelayCommand(CanExecute = nameof(CanApproveWrd))]
    private Task ApproveWrdAsync() => Run(SelectedWrd, line => _handlers.Resolve.HandleAsync(new ResolveLine(line.Id,
        IsCustomerAnswer ? ConfirmationBases.CustomerAnswer : ConfirmationBases.PreviousRealization, WrdText,
        WrdChoices.Select(c => c.ToResolution()).ToList())), "Zatwierdzono");
    private bool CanApproveWrd() => SelectedWrd is { Confirmed: false };

    [RelayCommand]
    private void ExportDifferenceList()
    {
        if (_workbook is null || _files is null) return;
        var rows = DifferenceList.Select(l => l.Wrd).ToList();
        if (rows.Count == 0) { Status = "Wykaz Różnic jest pusty."; return; }
        var name = $"Wykaz Różnic {Package.Code}" + (string.IsNullOrWhiteSpace(Package.CustomerRevision) ? "" : $" rev {Package.CustomerRevision}") + ".xlsx";
        if (_files.SaveExcel(name, _workbook.Write("Wykaz Różnic", rows)))
            Status = $"Zapisano Wykaz Różnic ({rows.Count} pozycji) do pliku Excel.";
    }

    [RelayCommand]
    private async Task ImportAnswersAsync()
    {
        if (_workbook is null || _files is null) return;
        try
        {
            if (_files.OpenExcel() is not { } content) return;
            var answers = _workbook.ReadAnswers(content);
            int matched = 0, failed = 0;
            foreach (var (designators, answer) in answers)
            {
                var line = DifferenceList.FirstOrDefault(l => l.Wrd.DesignatorsBom == designators && !l.Confirmed);
                if (line is null) { failed++; continue; }
                var result = await _handlers.Answer.HandleAsync(new RecordCustomerAnswer(line.Id, answer));
                if (result.IsSuccess) matched++; else failed++;
            }
            await ReloadAsync();
            Status = $"Wczytano odpowiedzi Klienta: {matched}" + (failed > 0 ? $"; nie dopasowano: {failed} (inne desygnatory albo pozycja już zatwierdzona)." : ".")
                     + " Zatwierdź każdą pozycję w panelu po prawej.";
        }
        catch (Exception e) { Status = ErrorMessages.Describe(e); }
    }

    // ── P&P i wersje ──

    [RelayCommand(CanExecute = nameof(CanApprovePnpNow))]
    private Task ApprovePnpAsync() => SelectedRevision is { } r ? Run(null, _ => _handlers.ApprovePnp.HandleAsync(new ApprovePnp(r.Id)), "Zatwierdzono P&P") : Task.CompletedTask;

    [RelayCommand]
    private Task ApproveBomAsync() => SelectedRevision is { } r
        ? Run(null, _ => _handlers.ApproveBom.HandleAsync(new ApproveBom(r.Id, ApproveForOrder ? BomVersion.ForOrder : BomVersion.Base, ApproveForOrder ? OrderNumber : null, ApproveComment)), "Zatwierdzono wersję BOM")
        : Task.CompletedTask;

    private async Task Run(BomLineItem? line, Func<BomLineItem, Task<Result>> command, string done)
    {
        try
        {
            var result = await command(line!);
            if (!result.IsSuccess) { Status = result.Error ?? "Operacja nie powiodła się."; await ReloadAsync(); return; }
            Status = line is null ? done + "." : $"{done} (linia {line.LineNumber}).";
            await ReloadAsync();
        }
        catch (Exception e) { Status = ErrorMessages.Describe(e); }
    }

    private void ApplyFilter()
    {
        var selected = SelectedLine?.Id;
        var text = Search.Trim();
        Lines.Clear();
        foreach (var line in _all.Where(l => l.Matches(text) && (!OnlyDifferences || l.HasDifferences)))
            Lines.Add(line);
        if (selected is not null) SelectedLine = Lines.FirstOrDefault(l => l.Id == selected);
    }
}
