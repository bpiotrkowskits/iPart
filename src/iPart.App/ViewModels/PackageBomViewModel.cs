using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iPart.Core.Components;
using iPart.Core.Packages;
using ProduMan.SharedKernel.Common;

namespace iPart.App.ViewModels;

/// <summary>Handlery komend procesu BOM — ViewModel wywołuje je przez interfejsy, nigdy bazę (ADR-0014).</summary>
public sealed record BomHandlers(
    IConfirmBomLineHandler Confirm,
    ICommandHandler<SetComparisonDecision> Decide,
    ICommandHandler<RecordCustomerAnswer> Answer,
    ICommandHandler<UpdateLineTs> UpdateTs,
    ICommandHandler<AddLineMpn> AddMpn,
    ICommandHandler<RemoveLineMpn> RemoveMpn,
    ICommandHandler<ApprovePnp> ApprovePnp,
    ICommandHandler<ApproveBom> ApproveBom,
    ICommandHandler<AttachPnp> AttachPnp);

/// <summary>Linia BOM do wyświetlenia: Oryginał Klienta, sekcja TS, porównanie z P&amp;P, MPN, potwierdzenie, kolor (8.1) i podpowiedź (8.4).</summary>
public sealed class BomLineItem(BomLine line, LineComparison? comparison = null, IReadOnlyList<LineMpn>? mpns = null)
{
    public BomLine Line => line;
    public Guid Id => line.Id;
    public int LineNumber => line.LineNumber;
    public string? Designators => line.Designators;
    public int DesignatorCount => line.ExpandedDesignators.Count;
    public int? Quantity => line.Quantity;
    public string? PartTs => line.PartTs;
    public string? MountingCode => line.MountingCode;
    public string? Footprint => line.Footprint;
    public string? Description => line.Description;
    public string? Value => line.Value;
    public string? PartNumber => line.PartNumber;
    public string? Notes => line.Notes;
    public bool Confirmed => line.Confirmed;
    public string? Question => line.DifferenceQuestion;
    public string? CustomerAnswer => line.CustomerAnswer;
    public bool InDifferenceList => line.ComparisonDecision == ComparisonDecisions.DifferenceList;
    public string Extras { get; } = string.Join("; ", line.ExtraValues.OrderBy(x => x.Ordinal).Select(x => $"{x.Header}: {x.Value}"));
    public IReadOnlyList<LineDifference> Differences { get; } = comparison?.Differences ?? [];
    public IReadOnlyList<LineMpn> Approved { get; } = (mpns ?? []).Where(m => !m.Forbidden).OrderBy(m => m.Ordinal).ToList();
    public IReadOnlyList<LineMpn> Forbidden { get; } = (mpns ?? []).Where(m => m.Forbidden).ToList();
    public string MpnSummary => Forbidden.Count == 0 ? $"{Approved.Count} zatw." : $"{Approved.Count} zatw. · {Forbidden.Count} niedozw.";
    public string State => Confirmed ? "potwierdzona" : "niepotwierdzona";

    public string ComparisonText =>
        line.ComparisonDecision == ComparisonDecisions.DifferenceList ? (CustomerAnswer is null ? "Wykaz Różnic — czeka na Klienta" : "Wykaz Różnic — jest odpowiedź")
        : line.ComparisonDecision == ComparisonDecisions.Checked ? "sprawdzone"
        : Differences.Count == 0 ? "zgodne"
        : string.Join(", ", Differences.Select(d => d.Kind));

    /// <summary>Kolor wiersza; kolejność przy kilku znaczeniach: DNP → niepotwierdzona → montaż inny niż SMT (8.4).</summary>
    public string ColorKey => MountingCode == "DNP" ? "Dnp" : !Confirmed ? "Unconfirmed" : MountingCategories.IsNonSmt(MountingCode) ? "NonSmt" : "";

    public string ConfirmationText => !Confirmed ? "Linia niepotwierdzona."
        : $"Potwierdzona — {ConfirmationBases.Describe(line.ConfirmationBasis)}"
          + (string.IsNullOrWhiteSpace(line.ConfirmationNote) ? "" : $": {line.ConfirmationNote}")
          + $" ({line.ConfirmedBy}, {line.ConfirmedAtUtc?.ToLocalTime():dd.MM.yyyy HH:mm})";

    public string Tooltip => $"Linia {LineNumber}: {State}" + (MountingCode is null ? "" : $", montaż {MountingCode}")
        + $". Porównanie z P&P: {ComparisonText}. MPN: {MpnSummary}.";

    public bool Matches(string text) => text.Length == 0
        || (Designators?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)
        || (PartNumber?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)
        || (Description?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)
        || (Value?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)
        || (PartTs?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false);
}

public sealed record BasisOption(string Code, string Name);
public sealed record LineFilter(string Id, string Name);

/// <summary>
/// BOM pakietu — cały proces Opiekuna Projektu na jednej stronie (makieta „4. BOM pakietu” i „5. Zatwierdzenie”):
/// linie z porównaniem z P&amp;P, decyzja i Wykaz Różnic, sekcja TS i MPN, potwierdzenie z podstawą,
/// zatwierdzenie P&amp;P (Technolog SMT) i zatwierdzenie BOM (wersja). Wszystkie zapisy przez handlery komend.
/// </summary>
public sealed partial class PackageBomViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private readonly IPackageService _packages;
    private readonly IBomWorkflowReader _reader;
    private readonly BomHandlers _handlers;
    private readonly IPackageHistoryReader _history;
    private readonly Action _back;
    private readonly Action<PackageBomViewModel>? _openPnpImport;
    private readonly Action<string>? _copy;
    private IReadOnlyList<BomLineItem> _all = [];

    public PackageBomViewModel(PackageSummary package, IPackageService packages, IBomWorkflowReader reader, BomHandlers handlers,
        IPackageHistoryReader history, Action back, Action<PackageBomViewModel>? openPnpImport = null, Action<string>? copy = null)
    {
        Package = package;
        _packages = packages;
        _reader = reader;
        _handlers = handlers;
        _history = history;
        _back = back;
        _openPnpImport = openPnpImport;
        _copy = copy;
    }

    public PackageSummary Package { get; }
    public ObservableCollection<BomRevisionSummary> Revisions { get; } = [];
    public ObservableCollection<BomLineItem> Lines { get; } = [];
    public ObservableCollection<BomLineItem> DifferenceList { get; } = [];
    public ObservableCollection<string> OnlyInPnp { get; } = [];
    public ObservableCollection<HistoryEntry> History { get; } = [];
    public ObservableCollection<BomVersion> Versions { get; } = [];
    public ObservableCollection<MountingOption> MountingOptions { get; } = [];
    public IReadOnlyList<BasisOption> Bases { get; } = ConfirmationBases.Names.Select(x => new BasisOption(x.Key, x.Value)).ToList();
    public IReadOnlyList<LineFilter> Filters { get; } =
        [new("all", "Wszystkie"), new("unconfirmed", "Niepotwierdzone"), new("differences", "Z rozbieżnościami"), new("wrd", "W Wykazie Różnic")];

    [ObservableProperty] public partial BomRevisionSummary? SelectedRevision { get; set; }
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConfirmCommand), nameof(SaveTsCommand), nameof(MarkCheckedCommand), nameof(AddToDifferenceListCommand), nameof(SaveAnswerCommand), nameof(AddMpnCommand), nameof(RemoveMpnCommand))]
    public partial BomLineItem? SelectedLine { get; set; }
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))] public partial BasisOption? SelectedBasis { get; set; }
    [ObservableProperty] public partial string Note { get; set; } = "";
    [ObservableProperty] public partial string Search { get; set; } = "";
    [ObservableProperty] public partial LineFilter? SelectedFilter { get; set; }
    [ObservableProperty] public partial string Status { get; set; } = "";
    [ObservableProperty] public partial string Summary { get; set; } = "";
    [ObservableProperty] public partial string PnpState { get; set; } = "";
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ApprovePnpCommand))] public partial bool CanApprovePnpNow { get; set; }
    // Edycja linii
    [ObservableProperty] public partial string Question { get; set; } = "";
    [ObservableProperty] public partial string Answer { get; set; } = "";
    [ObservableProperty] public partial string EditPartTs { get; set; } = "";
    [ObservableProperty] public partial MountingOption? EditMounting { get; set; }
    [ObservableProperty] public partial string NewMpn { get; set; } = "";
    [ObservableProperty] public partial bool NewMpnForbidden { get; set; }
    [ObservableProperty] public partial string NewMpnNote { get; set; } = "";
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(RemoveMpnCommand))] public partial LineMpn? SelectedMpn { get; set; }
    [ObservableProperty] public partial string RemoveReason { get; set; } = "";
    // Zatwierdzenie BOM
    [ObservableProperty] public partial bool ApproveForOrder { get; set; } = true;
    [ObservableProperty] public partial string OrderNumber { get; set; } = "";
    [ObservableProperty] public partial string ApproveComment { get; set; } = "";
    [ObservableProperty] public partial string ApproveConditions { get; set; } = "";

    partial void OnSelectedRevisionChanged(BomRevisionSummary? value) => _ = ReloadAsync(null);
    partial void OnSearchChanged(string value) => ApplyFilter();
    partial void OnSelectedFilterChanged(LineFilter? value) => ApplyFilter();
    partial void OnSelectedLineChanged(BomLineItem? value)
    {
        SelectedBasis = value?.InDifferenceList == true && value.CustomerAnswer is not null ? Bases.First(b => b.Code == ConfirmationBases.CustomerAnswer) : null;
        Note = value?.CustomerAnswer ?? "";
        Question = value?.Question ?? "";
        Answer = value?.CustomerAnswer ?? "";
        EditPartTs = value?.PartTs ?? "";
        EditMounting = MountingOptions.FirstOrDefault(m => m.Code == value?.MountingCode);
        NewMpn = NewMpnNote = RemoveReason = "";
        SelectedMpn = null;
    }

    public async Task LoadAsync(Guid? revisionId = null)
    {
        try
        {
            SelectedFilter ??= Filters[0];
            if (MountingOptions.Count == 0) foreach (var m in await _reader.MountingMethodsAsync()) MountingOptions.Add(m);
            Revisions.Clear();
            foreach (var r in await _packages.ListRevisionsAsync(Package.Id)) Revisions.Add(r);
            if (Revisions.Count == 0) Status = "Pakiet nie ma jeszcze BOM — zaimportuj BOM Klienta (menu „Import BOM”).";
            var target = Revisions.FirstOrDefault(r => r.Id == revisionId) ?? Revisions.FirstOrDefault();
            if (target == SelectedRevision) await ReloadAsync(null); else SelectedRevision = target;
        }
        catch (Exception e) { Status = ErrorMessages.Describe(e); }
    }

    /// <summary>Ponowne wczytanie linii, P&amp;P, MPN, wersji i historii — po każdej zapisanej komendzie.</summary>
    public async Task ReloadAsync(Guid? selectLine)
    {
        try
        {
            selectLine ??= SelectedLine?.Id;
            if (SelectedRevision is not { } revision) { _all = []; ApplyFilter(); return; }
            var lines = await _packages.GetLinesAsync(revision.Id);
            var pnp = await _reader.PnpRowsAsync(revision.Id);
            var mpns = await _reader.MpnsAsync(revision.Id);
            var comparison = BomPnpComparer.Compare(lines, pnp);
            _all = lines.Select(l => new BomLineItem(l, comparison.Lines.FirstOrDefault(c => c.LineId == l.Id), mpns.Where(m => m.BomLineId == l.Id).ToList())).ToList();
            ApplyFilter();
            SelectedLine = Lines.FirstOrDefault(l => l.Id == selectLine);

            OnlyInPnp.Clear();
            foreach (var d in comparison.OnlyInPnp) OnlyInPnp.Add(d);
            DifferenceList.Clear();
            foreach (var l in _all.Where(l => l.InDifferenceList)) DifferenceList.Add(l);

            var unconfirmed = _all.Count(l => !l.Confirmed);
            Summary = $"{_all.Count} linii · niepotwierdzone: {unconfirmed} · rozbieżności z P&P: {comparison.Differences} · w Wykazie Różnic: {DifferenceList.Count}";
            var full = await ReloadRevisionAsync(revision.Id);
            PnpState = full?.PnpFileName is null ? "P&P: brak — zaimportuj plik P&P"
                : full.PnpApprovedAtUtc is null ? $"P&P: {full.PnpFileName} ({pnp.Count} wierszy) — czeka na zatwierdzenie przez Technologa SMT"
                : $"P&P: {full.PnpFileName} — zatwierdzone ({full.PnpApprovedBy}, {full.PnpApprovedAtUtc?.ToLocalTime():dd.MM.yyyy HH:mm})";
            CanApprovePnpNow = full?.PnpFileName is not null && full.PnpApprovedAtUtc is null;
            ApproveConditions = string.Join(Environment.NewLine, new[]
            {
                unconfirmed == 0 ? "✓ Wszystkie linie potwierdzone." : $"! {unconfirmed} linii niepotwierdzonych — wejdą do wersji, ale zakupy ich nie liczą, dopóki ich nie potwierdzisz.",
                DifferenceList.Any(l => l.CustomerAnswer is null) ? $"! {DifferenceList.Count(l => l.CustomerAnswer is null)} pytań w Wykazie Różnic bez odpowiedzi Klienta." : "✓ Wykaz Różnic bez otwartych pytań.",
                full?.PnpApprovedAtUtc is not null ? "✓ P&P zatwierdzone." : "! P&P niezatwierdzone — nie blokuje BOM, blokuje gotowość dla produkcji SMT.",
            });

            Versions.Clear();
            foreach (var v in await _reader.VersionsAsync(Package.Id)) Versions.Add(v);
            History.Clear();
            foreach (var entry in await _history.ListAsync(Package.Id)) History.Add(entry);
        }
        catch (Exception e) { Status = ErrorMessages.Describe(e); }
    }

    // Dane rewizji (P&P, zatwierdzenie P&P) — serwis odczytu.
    private Task<BomRevision?> ReloadRevisionAsync(Guid revisionId) => _reader.RevisionAsync(revisionId);

    [RelayCommand] private void Back() => _back();
    [RelayCommand] private void ImportPnp() => _openPnpImport?.Invoke(this);

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private Task ConfirmAsync() => Run(SelectedLine, line => _handlers.Confirm.HandleAsync(new ConfirmBomLine(line.Id, SelectedBasis!.Code, Note)), "Potwierdzono");
    private bool CanConfirm() => SelectedLine is { Confirmed: false } && SelectedBasis is not null;

    [RelayCommand(CanExecute = nameof(CanDecide))]
    private Task MarkCheckedAsync() => Run(SelectedLine, line => _handlers.Decide.HandleAsync(new SetComparisonDecision(line.Id, ComparisonDecisions.Checked, null)), "Oznaczono jako sprawdzone");

    [RelayCommand(CanExecute = nameof(CanDecide))]
    private Task AddToDifferenceListAsync() => Run(SelectedLine, line => _handlers.Decide.HandleAsync(new SetComparisonDecision(line.Id, ComparisonDecisions.DifferenceList, Question)), "Dodano do Wykazu Różnic");
    private bool CanDecide() => SelectedLine is { Confirmed: false };

    [RelayCommand(CanExecute = nameof(CanAnswer))]
    private Task SaveAnswerAsync() => Run(SelectedLine, line => _handlers.Answer.HandleAsync(new RecordCustomerAnswer(line.Id, Answer)), "Zapisano odpowiedź Klienta");
    private bool CanAnswer() => SelectedLine is { InDifferenceList: true };

    [RelayCommand(CanExecute = nameof(HasLine))]
    private Task SaveTsAsync() => Run(SelectedLine, line => _handlers.UpdateTs.HandleAsync(new UpdateLineTs(line.Id, EditPartTs, EditMounting?.Code, null)), "Zapisano sekcję TS");

    [RelayCommand(CanExecute = nameof(HasLine))]
    private Task AddMpnAsync() => Run(SelectedLine, line => _handlers.AddMpn.HandleAsync(new AddLineMpn(line.Id, NewMpn, NewMpnForbidden, NewMpnNote)),
        NewMpnForbidden ? "Dodano MPN niedozwolony" : "Dodano MPN zatwierdzony");
    private bool HasLine() => SelectedLine is not null;

    [RelayCommand(CanExecute = nameof(CanRemoveMpn))]
    private Task RemoveMpnAsync() => Run(SelectedLine, _ => _handlers.RemoveMpn.HandleAsync(new RemoveLineMpn(SelectedMpn!.Id, RemoveReason)), "Usunięto MPN z listy");
    private bool CanRemoveMpn() => SelectedLine is not null && SelectedMpn is not null;

    [RelayCommand(CanExecute = nameof(CanApprovePnpNow))]
    private Task ApprovePnpAsync() => SelectedRevision is { } r ? Run(null, _ => _handlers.ApprovePnp.HandleAsync(new ApprovePnp(r.Id)), "Zatwierdzono P&P") : Task.CompletedTask;

    [RelayCommand]
    private Task ApproveBomAsync() => SelectedRevision is { } r
        ? Run(null, _ => _handlers.ApproveBom.HandleAsync(new ApproveBom(r.Id, ApproveForOrder ? BomVersion.ForOrder : BomVersion.Base, ApproveForOrder ? OrderNumber : null, ApproveComment)), "Zatwierdzono wersję BOM")
        : Task.CompletedTask;

    [RelayCommand]
    private void CopyDifferenceList()
    {
        var text = string.Join(Environment.NewLine, DifferenceList.Select(l => $"Linia {l.LineNumber} ({l.Designators}): {l.Question}" + (l.CustomerAnswer is null ? "" : $" — odpowiedź: {l.CustomerAnswer}")));
        _copy?.Invoke($"Wykaz Różnic Dokumentacyjnych — {Package.DisplayName}{Environment.NewLine}{text}");
        Status = $"Skopiowano Wykaz Różnic ({DifferenceList.Count} pozycji) do schowka.";
    }

    private async Task Run(BomLineItem? line, Func<BomLineItem, Task<Result>> command, string done)
    {
        try
        {
            var result = await command(line!);
            if (!result.IsSuccess) { Status = result.Error ?? "Operacja nie powiodła się."; return; }
            Status = line is null ? done + "." : $"{done} (linia {line.LineNumber}).";
            await ReloadAsync(line?.Id);
        }
        catch (Exception e) { Status = ErrorMessages.Describe(e); }
    }

    private void ApplyFilter()
    {
        var selected = SelectedLine?.Id;
        var text = Search.Trim();
        Lines.Clear();
        foreach (var line in _all.Where(l => l.Matches(text) && (SelectedFilter?.Id ?? "all") switch
                 {
                     "unconfirmed" => !l.Confirmed,
                     "differences" => l.Differences.Count > 0,
                     "wrd" => l.InDifferenceList,
                     _ => true
                 }))
            Lines.Add(line);
        if (selected is not null) SelectedLine = Lines.FirstOrDefault(l => l.Id == selected);
    }
}
