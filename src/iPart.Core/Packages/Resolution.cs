namespace iPart.Core.Packages;

/// <summary>Pola porównywane z P&amp;P w Comparatorze (kolumny Wykazu Różnic Dokumentacyjnych).</summary>
public static class ComparedFields
{
    /// <summary>Value — kolumna porównywana między BOM a P&amp;P (nie Part TS).</summary>
    public const string Value = "Value";
    public const string Footprint = "Footprint";
    public const string Description = "Description";
    public const string PartNumber = "PartNumber";

    public static readonly IReadOnlyList<string> All = [Value, Footprint, Description, PartNumber];

    public static string Label(string field) => field switch
    {
        Value => "Value",
        Footprint => "Footprint",
        Description => "Description",
        PartNumber => "PartNumber",
        _ => field
    };
}

/// <summary>Które pola Comparator bierze pod uwagę (wybór Opiekuna, zapisany przy rewizji importu).</summary>
public sealed record ComparatorOptions(bool Value, bool Footprint, bool Description, bool PartNumber = false)
{
    public static ComparatorOptions AllFields { get; } = new(true, true, true);

    public bool Includes(string field) => field switch
    {
        ComparedFields.Value => Value,
        ComparedFields.Footprint => Footprint,
        ComparedFields.Description => Description,
        ComparedFields.PartNumber => PartNumber,
        _ => false
    };

    /// <summary>Zapis w bazie: kody pól po przecinku; null albo pusty = wszystkie pola (stan sprzed wyboru).</summary>
    public static ComparatorOptions Parse(string? stored)
    {
        if (stored is null) return AllFields;
        var set = stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal);
        return new(set.Contains(ComparedFields.Value), set.Contains(ComparedFields.Footprint), set.Contains(ComparedFields.Description), set.Contains(ComparedFields.PartNumber));
    }

    public string Store() => string.Join(",", ComparedFields.All.Where(Includes));
    public string Describe() => ComparedFields.All.Where(Includes).Select(ComparedFields.Label) is var l && l.Any() ? string.Join(", ", l) : "żadne pole";
}

/// <summary>Skąd wziąć wartość: z BOM Klienta, z P&amp;P Klienta albo wpisaną przez Opiekuna.</summary>
public enum ValueSource { Bom, Pnp, Own }

/// <summary>Wartości jednego pola linii: BOM i P&amp;P (różne wartości P&amp;P dla desygnatorów linii).</summary>
public sealed record FieldValues(string Field, string? Bom, IReadOnlyList<string> Pnp, bool Compared = true)
{
    public string Label => ComparedFields.Label(Field);
    public string? PnpSingle => Pnp.Count == 1 ? Pnp[0] : null;
    public string PnpText => Pnp.Count == 0 ? "—" : string.Join(" | ", Pnp);
    /// <summary>Pusty Footprint w BOM i jeden Footprint w P&amp;P — przyjmowany z P&amp;P automatycznie, to nie różnica.</summary>
    public bool TakenFromPnp => Field == ComparedFields.Footprint && Bom is null && Pnp.Count == 1;
    /// <summary>Pusty Footprint w BOM i kilka różnych w P&amp;P — linię trzeba rozdzielić według grup P&amp;P.</summary>
    public bool NeedsSplit => Field == ComparedFields.Footprint && Bom is null && Pnp.Count > 1;
    public bool Differs => Compared && Pnp.Count > 0 && !TakenFromPnp
        && (Bom is null || Pnp.Count > 1 || !BomPnpComparer.Same(Field, Bom, Pnp[0]));
}

/// <summary>Wybór wartości dla pola przy rozstrzygnięciu linii.</summary>
public sealed record FieldResolution(string Field, ValueSource Source, string? OwnValue);

/// <summary>
/// Rozstrzygnięcie linii po porównaniu z P&amp;P: podstawa (decyzja Opiekuna, odpowiedź Klienta, wcześniejsza realizacja),
/// odpowiedź Klienta albo komentarz, wybór wartości dla różniących się pól — i zatwierdzenie linii.
/// </summary>
public sealed record ResolveLine(Guid LineId, string Basis, string? Note, IReadOnlyList<FieldResolution> Resolutions);

/// <summary>Comparator: zmiana pól branych pod uwagę; linie zgodne zatwierdzają się same, niezgodne wracają do sprawdzenia.</summary>
public sealed record SetComparatorOptions(Guid RevisionId, ComparatorOptions Options);

/// <summary>Rozdziel linię według grup P&amp;P (np. R1–R5 0402 i R6–R10 0603) — nowe linie na końcu, z odnośnikiem do źródłowej.</summary>
public sealed record SplitLine(Guid LineId);

/// <summary>Powrót z kroku 2 (Import P&P) do kroku 1: wczytany BOM jest usuwany — zmiany przepadają.</summary>
public sealed record DiscardBomRevision(Guid RevisionId);

/// <summary>Powrót z kroku 3 (Comparator) do kroku 2: P&P, decyzje, Wykaz Różnic i rozdzielenia linii są cofane — zmiany przepadają.</summary>
public sealed record ResetComparator(Guid RevisionId);

public static class LineFields
{
    /// <summary>
    /// Wartości pól linii: Value, Footprint i Description — BOM i różne wartości P&amp;P dla desygnatorów linii.
    /// Pola niewybrane w Comparatorze są pokazywane, ale nie liczą się jako różnica.
    /// </summary>
    public static IReadOnlyList<FieldValues> For(BomLine line, IReadOnlyList<PnpRow> pnp, ComparatorOptions? options = null)
    {
        options ??= ComparatorOptions.AllFields;
        var rows = RowsOf(line, pnp);
        IReadOnlyList<string> Distinct(Func<PnpRow, string?> pick) => rows.Select(pick).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim())
            .GroupBy(BomPnpComparer.Normalize).Select(g => g.First()).ToList();
        return
        [
            new(ComparedFields.Value, Clean(line.Value), Distinct(p => p.Value), options.Value),
            new(ComparedFields.Footprint, Clean(line.Footprint), Distinct(p => p.Footprint), options.Footprint),
            new(ComparedFields.Description, Clean(line.Description), Distinct(p => p.Description), options.Description),
            new(ComparedFields.PartNumber, Clean(line.PartNumber), Distinct(p => p.PartNumber), options.PartNumber),
        ];
    }

    /// <summary>Wiersze P&amp;P desygnatorów linii.</summary>
    public static IReadOnlyList<PnpRow> RowsOf(BomLine line, IReadOnlyList<PnpRow> pnp)
    {
        var designators = line.ExpandedDesignators.Select(d => BomPnpComparer.Normalize(d.Designator)).ToHashSet();
        return pnp.Where(p => designators.Contains(BomPnpComparer.Normalize(p.Designator))).ToList();
    }

    /// <summary>Wartość wybrana; null, gdy wybór jest niemożliwy (np. kilka różnych wartości w P&amp;P).</summary>
    public static string? Value(FieldValues values, FieldResolution resolution) => resolution.Source switch
    {
        ValueSource.Bom => values.Bom,
        ValueSource.Pnp => values.PnpSingle,
        _ => string.IsNullOrWhiteSpace(resolution.OwnValue) ? null : resolution.OwnValue.Trim()
    };

    /// <summary>
    /// Grupy do rozdzielenia linii: desygnatory obecne w P&amp;P grupowane po wartościach wybranych pól (Footprint, Value, Description).
    /// Desygnatory bez P&amp;P zostają w pierwszej grupie. Jedna grupa = nie ma czego rozdzielać.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<string>> SplitGroups(BomLine line, IReadOnlyList<PnpRow> pnp, ComparatorOptions? options = null)
    {
        options ??= ComparatorOptions.AllFields;
        var byDesignator = pnp.GroupBy(p => BomPnpComparer.Normalize(p.Designator)).ToDictionary(g => g.Key, g => g.First());
        string Key(PnpRow r) => string.Join("|",
            options.Footprint || line.Footprint is null ? BomPnpComparer.Normalize(r.Footprint) : "",
            options.Value ? BomPnpComparer.Normalize(r.Value) : "",
            options.Description ? BomPnpComparer.Normalize(r.Description) : "",
            options.PartNumber ? BomPnpComparer.Normalize(r.PartNumber) : "");
        var withPnp = line.ExpandedDesignators.Where(d => byDesignator.ContainsKey(BomPnpComparer.Normalize(d.Designator))).ToList();
        var without = line.ExpandedDesignators.Where(d => !byDesignator.ContainsKey(BomPnpComparer.Normalize(d.Designator))).Select(d => d.Designator).ToList();
        var groups = withPnp.GroupBy(d => Key(byDesignator[BomPnpComparer.Normalize(d.Designator)]))
            .Select(g => DesignatorRanges.Sort(g.Select(d => d.Designator)).ToList())
            .OrderByDescending(g => g.Count).ToList();
        if (groups.Count == 0) return [without];
        groups[0] = DesignatorRanges.Sort(groups[0].Concat(without)).ToList();
        return groups;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Wiersz Wykazu Różnic Dokumentacyjnych — układ jak w firmowym wzorze (zrodla/Wykaz Różnic Dokumentacyjnych.xlsx).</summary>
public sealed record DifferenceListRow(
    int LineNumber, string DesignatorsBom, string DesignatorsPnp,
    string? PartTsBom, string? PartTsPnp, string? FootprintBom, string? FootprintPnp,
    string? DescriptionBom, string? DescriptionPnp, string? Question, string? Answer)
{
    public static DifferenceListRow For(BomLine line, IReadOnlyList<PnpRow> pnp)
    {
        var designators = line.ExpandedDesignators.Select(d => d.Designator).ToList();
        var inPnp = pnp.Select(p => BomPnpComparer.Normalize(p.Designator)).ToHashSet();
        var fields = LineFields.For(line, pnp);
        string? Pnp(string field) => fields.Single(f => f.Field == field) is { Pnp.Count: > 0 } f ? f.PnpText : null;
        return new(line.LineNumber,
            DesignatorRanges.Compress(designators),
            DesignatorRanges.Compress(designators.Where(d => inPnp.Contains(BomPnpComparer.Normalize(d)))),
            line.Value, Pnp(ComparedFields.Value), line.Footprint, Pnp(ComparedFields.Footprint),
            line.Description, Pnp(ComparedFields.Description), line.DifferenceQuestion, line.CustomerAnswer);
    }
}

/// <summary>Plik Wykazu Różnic: zapis do wysłania Klientowi i odczyt odpowiedzi z pliku odesłanego przez Klienta.</summary>
public interface IDifferenceListWorkbook
{
    byte[] Write(string title, IReadOnlyList<DifferenceListRow> rows);
    /// <summary>Odpowiedzi: Designator BOM (jak w wysłanym pliku) → odpowiedź Klienta. Wiersze bez odpowiedzi pomijane.</summary>
    IReadOnlyList<(string DesignatorsBom, string Answer)> ReadAnswers(byte[] content);
}

// ───────────── iPart roboczy: kolumny rdzenia BOM ─────────────

/// <summary>Dane komponentu z bazy dla MPN (Case, Footprint, Value z parametrów).</summary>
public sealed record ComponentCore(string Mpn, string? Case, string? Footprint, string? Value);

/// <summary>Wyszukanie komponentów w bazie po MPN (dokładne dopasowanie).</summary>
public interface IComponentLookup
{
    Task<IReadOnlyDictionary<string, ComponentCore>> FindAsync(IReadOnlyCollection<string> mpns, CancellationToken cancellationToken = default);
}

/// <summary>Wartość kolumny rdzenia BOM z informacją, skąd pochodzi.</summary>
public sealed record CoreValue(string? Value, string Source)
{
    public override string ToString() => Value ?? "";
}

/// <summary>
/// Kolumny rdzenia BOM w iParcie roboczym:
/// Case — z bazy (po PartNumber) albo z danych Klienta (Footprint ustalony w Comparatorze);
/// Footprint — z bazy albo duplikat Case; Value — z bazy albo z danych Klienta; PartNumber — z danych Klienta.
/// </summary>
public sealed record LineCore(CoreValue Case, CoreValue Footprint, CoreValue Value, string? PartNumber, string? Mpn)
{
    public const string FromDatabase = "baza komponentów";
    public const string FromCustomer = "dane Klienta";
    public const string FromCase = "jak Case";

    public static LineCore For(BomLine line, IReadOnlyList<LineMpn> mpns, IReadOnlyDictionary<string, ComponentCore> components)
    {
        // Pierwszy MPN z bazy komponentów (kolejność listy zatwierdzonych), inaczej pierwszy z BOM Klienta.
        var approved = mpns.Where(m => !m.Forbidden).OrderBy(m => m.Ordinal).Select(m => m.Mpn).ToList();
        var component = approved.Select(m => components.GetValueOrDefault(m)).FirstOrDefault(c => c is not null);
        var customerCase = line.TsFootprint ?? Clean(line.Footprint);
        var caseValue = component?.Case is { } dbCase ? new CoreValue(dbCase, FromDatabase) : new CoreValue(customerCase, FromCustomer);
        var footprint = component?.Footprint is { } dbFootprint ? new CoreValue(dbFootprint, FromDatabase) : new CoreValue(caseValue.Value, FromCase);
        var value = component?.Value is { } dbValue ? new CoreValue(dbValue, FromDatabase) : new CoreValue(line.TsValue ?? Clean(line.Value), FromCustomer);
        return new(caseValue, footprint, value, line.TsPartNumber ?? line.PartNumber, component?.Mpn ?? approved.FirstOrDefault());
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
