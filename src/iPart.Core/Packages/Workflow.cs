using System.Globalization;
using iPart.Core.Import;
using ProduMan.SharedKernel.Common;

namespace iPart.Core.Packages;

// ───────────── P&P ─────────────

public enum PnpField { None, Designator, X, Y, Rotation, Side, Footprint, Value, Description, PartNumber }

public static class PnpFields
{
    public static string Label(PnpField field) => field switch
    {
        PnpField.None => "(pomiń)",
        PnpField.Designator => "Designator",
        PnpField.X => "X",
        PnpField.Y => "Y",
        PnpField.Rotation => "Obrót",
        PnpField.Side => "Strona",
        PnpField.Footprint => "Footprint",
        PnpField.Value => "Value",
        PnpField.Description => "Description",
        PnpField.PartNumber => "PartNumber",
        _ => field.ToString()
    };
}

/// <summary>Wiersz P&amp;P Klienta w brzmieniu oryginalnym (sekcja Oryginał), przypięty do rewizji importu.</summary>
public sealed class PnpRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BomRevisionId { get; set; }
    public int SourceRow { get; set; }
    public string Designator { get; set; } = "";
    public string? X { get; set; }
    public string? Y { get; set; }
    /// <summary>Współrzędne przeliczone na mm (oryginał Klienta zostaje w X i Y).</summary>
    [Components.DecimalSpec(12, 4, AllowNegative = true)] public decimal? XMm { get; set; }
    [Components.DecimalSpec(12, 4, AllowNegative = true)] public decimal? YMm { get; set; }
    public string? Rotation { get; set; }
    public string? Side { get; set; }
    public string? Footprint { get; set; }
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string? PartNumber { get; set; }
}

public sealed record PnpColumnAssignment(int ColumnIndex, PnpField Field);

public sealed class PnpImportResult
{
    public List<PnpRow> Rows { get; } = [];
    public List<BomImportIssue> Issues { get; } = [];
    public bool HasErrors => Issues.Any(x => x.Severity == ImportSeverity.Error);
}

/// <summary>Buduje wiersze P&amp;P z tabeli i przypisań kolumn. Dane Klienta zostają bez zmian.</summary>
public static class PnpImportBuilder
{
    public static PnpImportResult Build(BomSourceTable table, IReadOnlyList<PnpColumnAssignment> assignments, CoordinateUnit unit = CoordinateUnit.Auto)
    {
        var result = new PnpImportResult();
        var column = assignments.Where(a => a.Field != PnpField.None).GroupBy(a => a.Field).ToDictionary(g => g.Key, g => g.First().ColumnIndex);
        if (!column.ContainsKey(PnpField.Designator))
        {
            result.Issues.Add(new(ImportSeverity.Error, null, null, "Żadna kolumna P&P nie jest przypisana do pola Designator."));
            return result;
        }
        string? Cell(BomSourceRow row, PnpField field) =>
            column.TryGetValue(field, out var i) && i < row.Values.Count && !string.IsNullOrWhiteSpace(row.Values[i]) ? row.Values[i]!.Trim() : null;

        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in table.Rows)
        {
            var designator = Cell(row, PnpField.Designator);
            if (designator is null) continue;
            if (seen.TryGetValue(designator, out var first))
            {
                result.Issues.Add(new(ImportSeverity.Error, null, row.SourceRowNumber, $"Desygnator {designator} występuje w P&P dwa razy (wiersze {first} i {row.SourceRowNumber})."));
                continue;
            }
            seen[designator] = row.SourceRowNumber;
            result.Rows.Add(new PnpRow
            {
                SourceRow = row.SourceRowNumber, Designator = designator,
                X = Cell(row, PnpField.X), Y = Cell(row, PnpField.Y), Rotation = Cell(row, PnpField.Rotation),
                Side = Cell(row, PnpField.Side), Footprint = Cell(row, PnpField.Footprint), Value = Cell(row, PnpField.Value), Description = Cell(row, PnpField.Description), PartNumber = Cell(row, PnpField.PartNumber),
            });
            var added = result.Rows[^1];
            added.XMm = Coordinates.ToMillimeters(added.X, unit);
            added.YMm = Coordinates.ToMillimeters(added.Y, unit);
        }
        var unreadable = result.Rows.Count(r => (r.X is not null && r.XMm is null) || (r.Y is not null && r.YMm is null));
        if (unreadable > 0) result.Issues.Add(new(ImportSeverity.Warning, null, null, $"Nie odczytano współrzędnych jako liczb w {unreadable} wierszach — zostają w oryginale, bez przeliczenia."));
        if (result.Rows.Count == 0) result.Issues.Add(new(ImportSeverity.Error, null, null, "Plik P&P nie zawiera żadnego desygnatora."));
        return result;
    }
}

/// <summary>
/// Odczyt pliku tekstowego P&amp;P (zwykle .txt): rozpoznaje separator (tabulator, średnik, przecinek albo ciąg spacji)
/// i zwraca wiersze z numerami — wiersz nagłówków wybiera użytkownik, jak przy BOM.
/// </summary>
public static class PnpTextReader
{
    /// <summary>
    /// Odczyt P&amp;P tekstowego: separator tabulator, średnik, przecinek albo odstępy (wyrównane kolumny, np. Altium).
    /// Tekst w cudzysłowie to jedna komórka — także gdy ma spacje („"RES0402 1%"”), separatory albo przejście do nowej linii.
    /// Numer wiersza = numer linii pliku, w której zaczyna się wiersz.
    /// </summary>
    public static IReadOnlyList<(int RowNumber, IReadOnlyList<string?> Values)> Read(string text)
    {
        var records = Records(text.Replace("\r\n", "\n").Replace('\r', '\n'));
        var sample = records.Where(r => !string.IsNullOrWhiteSpace(r.Text)).Take(50).Select(r => Unquoted(r.Text)).ToList();
        char? separator = Count('\t') > 0 ? '\t' : Count(';') >= sample.Count / 2 && Count(';') > 0 ? ';' : Count(',') >= sample.Count / 2 && Count(',') > 0 ? ',' : null;
        var rows = new List<(int, IReadOnlyList<string?>)>();
        foreach (var (line, record) in records)
        {
            if (string.IsNullOrWhiteSpace(record)) continue;
            rows.Add((line, Split(record, separator).Select(c => (string?)c.Trim()).ToList()));
        }
        return rows;

        int Count(char c) => sample.Sum(l => l.Count(x => x == c));
    }

    // Wiersze pliku: nowa linia w cudzysłowie nie kończy wiersza (zamieniana na spację).
    private static List<(int Line, string Text)> Records(string text)
    {
        var records = new List<(int, string)>();
        var current = new System.Text.StringBuilder();
        int line = 1, start = 1;
        var quoted = false;
        foreach (var ch in text)
        {
            if (ch == '"') quoted = !quoted;
            if (ch == '\n')
            {
                line++;
                if (quoted) { current.Append(' '); continue; }
                records.Add((start, current.ToString()));
                current.Clear();
                start = line;
                continue;
            }
            current.Append(ch);
        }
        if (current.Length > 0) records.Add((start, current.ToString()));
        return records;
    }

    // Tekst bez fragmentów w cudzysłowie — do rozpoznania separatora (przecinki w opisach się nie liczą).
    private static string Unquoted(string record)
    {
        var result = new System.Text.StringBuilder();
        var quoted = false;
        foreach (var ch in record)
        {
            if (ch == '"') { quoted = !quoted; continue; }
            if (!quoted) result.Append(ch);
        }
        return result.ToString();
    }

    // Komórki wiersza; separator null = jeden lub więcej odstępów. Cudzysłów grupuje, sam nie trafia do wartości.
    private static List<string> Split(string record, char? separator)
    {
        var cells = new List<string>();
        var current = new System.Text.StringBuilder();
        var quoted = false;
        var hasContent = false;
        foreach (var ch in record)
        {
            if (ch == '"') { quoted = !quoted; hasContent = true; continue; }
            var isSeparator = !quoted && (separator is { } s ? ch == s : char.IsWhiteSpace(ch));
            if (isSeparator)
            {
                if (separator is not null || hasContent) { cells.Add(current.ToString()); current.Clear(); hasContent = false; }
                continue;
            }
            current.Append(ch);
            hasContent = true;
        }
        if (separator is not null || hasContent) cells.Add(current.ToString());
        return cells;
    }

    /// <summary>Tabela od wskazanego wiersza nagłówków (numer wiersza w pliku).</summary>
    public static BomSourceTable ToTable(IReadOnlyList<(int RowNumber, IReadOnlyList<string?> Values)> rows, int headerRow)
    {
        var columns = rows.Count == 0 ? 0 : rows.Max(r => r.Values.Count);
        var header = rows.FirstOrDefault(r => r.RowNumber == headerRow);
        var headers = Enumerable.Range(0, columns).Select(i =>
            header.Values is not null && i < header.Values.Count && !string.IsNullOrWhiteSpace(header.Values[i]) ? header.Values[i]!.Trim() : $"Kolumna {i + 1}").ToList();
        return new BomSourceTable(headers, rows.Where(r => r.RowNumber > headerRow).Select(r => new BomSourceRow(r.RowNumber, r.Values)).ToList());
    }
}

// ───────────── Porównanie BOM–P&P ─────────────

public enum DifferenceSeverity { Warning, Difference }

public sealed record LineDifference(DifferenceSeverity Severity, string Kind, string Detail);

/// <summary>Różnica jednego pola w grupie desygnatorów: wartość w BOM i w P&amp;P (pełny tekst).</summary>
public sealed record FieldDifference(string Field, string Bom, string Pnp)
{
    public string Label => ComparedFields.Label(Field);
    public string BomText => $"BOM: {Bom}";
    public string PnpText => $"P&P: {Pnp}";
}

/// <summary>
/// Grupa desygnatorów linii o tym samym wyniku porównania z P&amp;P (podlinia, dawniej „wycinek” comparatora).
/// <see cref="Designators"/> w zapisie zakresami (np. „C2A, C2B, R1–R5”); <see cref="Details"/> — różniące się pola (BOM i P&amp;P osobno);
/// <see cref="Note"/> — wynik bez różnic pól (np. „brak w P&amp;P”, „zgodne”, „Footprint z P&amp;P: 0402”).
/// </summary>
public sealed record DesignatorGroup(string Designators, int Count, bool Matches, string Result)
{
    public IReadOnlyList<FieldDifference> Details { get; init; } = [];
    public string? Note { get; init; }
}

public sealed record LineComparison(Guid LineId, int LineNumber, IReadOnlyList<LineDifference> Differences, IReadOnlyList<DesignatorGroup> Groups)
{
    public bool HasDifferences => Differences.Count > 0;
}

public sealed record BomPnpComparison(IReadOnlyList<LineComparison> Lines, IReadOnlyList<string> OnlyInPnp)
{
    public int Differences => Lines.Count(l => l.Differences.Any(d => d.Severity == DifferenceSeverity.Difference));
    public int Warnings => Lines.Count(l => l.Differences.Count > 0 && l.Differences.All(d => d.Severity == DifferenceSeverity.Warning));
    /// <summary>Desygnatory tylko w P&amp;P w zapisie zakresami.</summary>
    public string OnlyInPnpText => DesignatorRanges.Compress(OnlyInPnp);
}

/// <summary>
/// Porównanie BOM z P&amp;P (1.3, 1.4): desygnatory linii dzielone na grupy o tym samym wyniku — „zgodne”, „brak w P&amp;P”,
/// „Footprint P&amp;P X ≠ BOM Y”, „Value P&amp;P X ≠ BOM Y”. Brak danych po obu stronach to ostrzeżenie, nie rozbieżność;
/// Quantity różne od liczby desygnatorów — ostrzeżenie. Normalizacja tylko do porównania (1.2) — dane zostają oryginalne.
/// </summary>
public static class BomPnpComparer
{
    private sealed record Outcome(string Designator, string Key, string Kind, string Text, DifferenceSeverity? Severity, string? PnpFootprint = null)
    {
        public IReadOnlyList<FieldDifference> Details { get; init; } = [];
        public string? Note { get; init; }
    }

    public static BomPnpComparison Compare(IReadOnlyList<BomLine> lines, IReadOnlyList<PnpRow> pnp, ComparatorOptions? options = null)
    {
        options ??= ComparatorOptions.AllFields;
        var byDesignator = pnp.GroupBy(p => Normalize(p.Designator)).ToDictionary(g => g.Key, g => g.First());
        var inBom = new HashSet<string>();
        var result = new List<LineComparison>();
        foreach (var line in lines.OrderBy(l => l.LineNumber))
        {
            var designators = line.ExpandedDesignators.Select(d => d.Designator).ToList();
            foreach (var d in designators) inBom.Add(Normalize(d));
            var bom = new BomSide(Clean(line.Footprint), Clean(line.Value), Clean(line.Description), Clean(line.PartNumber));

            // Wynik dla każdego desygnatora, potem grupy desygnatorów o tym samym wyniku.
            var outcomes = designators.Select(d => Evaluate(d, pnp.Count > 0, byDesignator, bom, options)).ToList();
            var groups = outcomes.GroupBy(o => o.Key)
                .Select(g => (Group: new DesignatorGroup(DesignatorRanges.Compress(g.Select(x => x.Designator)), g.Count(), g.First().Severity is null, g.First().Text)
                              { Details = g.First().Details, Note = g.First().Note },
                              First: g.First()))
                .OrderBy(g => g.Group.Matches ? 1 : 0).ToList();

            var differences = groups.Where(g => g.First.Severity is not null)
                .Select(g => new LineDifference(g.First.Severity!.Value, g.First.Kind, $"{g.Group.Designators}: {g.Group.Result}")).ToList();
            // Pusty Footprint w BOM, a w P&P różne dla grup desygnatorów — linię trzeba rozdzielić.
            var pnpFootprints = groups.Where(g => g.First.PnpFootprint is not null).Select(g => Normalize(g.First.PnpFootprint)).Distinct().Count();
            if (bom.Footprint is null && pnpFootprints > 1)
                differences.Add(new(DifferenceSeverity.Difference, SplitKind, "Brak Footprint w BOM, w P&P różne dla grup desygnatorów — rozdziel linię."));
            if (line.Quantity is { } qty && qty != designators.Count && designators.Count > 0)
                differences.Add(new(DifferenceSeverity.Warning, "Ilość ≠ desygnatory", $"Quantity = {qty}, desygnatorów = {designators.Count}."));

            result.Add(new(line.Id, line.LineNumber, differences, groups.Select(g => g.Group).ToList()));
        }
        var onlyInPnp = pnp.Where(p => !inBom.Contains(Normalize(p.Designator))).Select(p => p.Designator).ToList();
        return new(result, DesignatorRanges.Sort(onlyInPnp));
    }

    /// <summary>Rodzaj różnicy: linię trzeba rozdzielić według grup P&amp;P.</summary>
    public const string SplitKind = "Rozdziel linię";

    private sealed record BomSide(string? Footprint, string? Value, string? Description, string? PartNumber);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Outcome Evaluate(string designator, bool hasPnp, IReadOnlyDictionary<string, PnpRow> byDesignator, BomSide bom, ComparatorOptions options)
    {
        if (!hasPnp) return new(designator, "nopnp", "", "brak P&P", null) { Note = "brak P&P" };
        if (!byDesignator.TryGetValue(Normalize(designator), out var row))
            return new(designator, "missing", "Brak w P&P", "brak w P&P", DifferenceSeverity.Difference) { Note = "brak w P&P" };
        var details = new List<FieldDifference>();
        var notes = new List<string>();
        void Check(string field, bool compared, string? bomValue, string? pnpValue)
        {
            if (compared && bomValue is not null && pnpValue is not null && !Same(field, bomValue, pnpValue))
                details.Add(new(field, bomValue, pnpValue));
        }
        var pnpFootprint = Clean(row.Footprint);
        Check(ComparedFields.Footprint, options.Footprint, bom.Footprint, pnpFootprint);
        // Pusty Footprint w BOM: przyjmujemy z P&P (grupa według Footprint P&P — różne grupy = rozdzielenie linii).
        if (bom.Footprint is null && pnpFootprint is not null) notes.Add($"Footprint z P&P: {pnpFootprint}");
        Check(ComparedFields.Value, options.Value, bom.Value, Clean(row.Value));
        Check(ComparedFields.Description, options.Description, bom.Description, Clean(row.Description));
        Check(ComparedFields.PartNumber, options.PartNumber, bom.PartNumber, Clean(row.PartNumber));
        var takenFootprint = bom.Footprint is null ? pnpFootprint : null;
        var note = notes.Count == 0 ? null : string.Join("; ", notes);
        var suffix = notes.Count == 0 ? "" : "|" + string.Join("|", notes);
        if (details.Count > 0)
        {
            var problems = details.Select(d => $"{d.Label} P&P {d.Pnp} ≠ BOM {d.Bom}").ToList();
            return new(designator, "diff|" + string.Join("|", problems) + suffix, string.Join(", ", details.Select(d => d.Label)),
                string.Join("; ", problems.Concat(notes)), DifferenceSeverity.Difference, takenFootprint) { Details = details, Note = note };
        }
        if (options.Footprint && bom.Footprint is null && pnpFootprint is null)
            return new(designator, "nodata", "Brak danych po obu stronach", "brak Footprint w BOM i w P&P", DifferenceSeverity.Warning) { Note = "brak Footprint w BOM i w P&P" };
        var text = notes.Count == 0 ? "zgodne" : "zgodne; " + note;
        return new(designator, "ok" + suffix, "", text, null, takenFootprint) { Note = text };
    }

    /// <summary>
    /// Równość do porównania: po normalizacji, cały tekst — także PartNumber (na tym etapie traktowany jako tekst, bez dzielenia na MPN).
    /// </summary>
    public static bool Same(string field, string bom, string pnp) =>
        Normalize(bom) == Normalize(pnp);

    /// <summary>Tylko do porównania: bez spacji i wielkości liter, µ (mikro) = μ (mu).</summary>
    public static string Normalize(string? value) =>
        string.Concat((value ?? "").Where(c => !char.IsWhiteSpace(c))).Replace('µ', 'μ').ToUpperInvariant();
}

/// <summary>Zapis desygnatorów zakresami do wyświetlania: R1, R2, R3, R5 → „R1–R3, R5” (ten sam prefiks, kolejne numery, bez sufiksu).</summary>
public static partial class DesignatorRanges
{
    [System.Text.RegularExpressions.GeneratedRegex(@"^(?<p>[A-Za-z_]+)(?<n>\d+)(?<s>[A-Za-z]*)$")]
    private static partial System.Text.RegularExpressions.Regex Parts();

    public static IReadOnlyList<string> Sort(IEnumerable<string> designators) => designators
        .Select(d => (d, m: Parts().Match(d)))
        .OrderBy(x => x.m.Success ? x.m.Groups["p"].Value.ToUpperInvariant() : x.d.ToUpperInvariant(), StringComparer.Ordinal)
        .ThenBy(x => x.m.Success && long.TryParse(x.m.Groups["n"].Value, out var n) ? n : long.MaxValue)
        .ThenBy(x => x.d, StringComparer.OrdinalIgnoreCase)
        .Select(x => x.d).ToList();

    public static string Compress(IEnumerable<string> designators)
    {
        var parts = new List<string>();
        string? start = null, end = null, prefix = null;
        long last = -2;
        var width = 0;
        foreach (var d in Sort(designators))
        {
            var m = Parts().Match(d);
            if (!m.Success || m.Groups["s"].Value.Length > 0 || !long.TryParse(m.Groups["n"].Value, out var n))
            {
                Flush(); prefix = null; last = -2; parts.Add(d); continue;
            }
            var p = m.Groups["p"].Value;
            var digits = m.Groups["n"].Value.Length;
            if (start is not null && string.Equals(p, prefix, StringComparison.OrdinalIgnoreCase) && n == last + 1 && (digits == width || !m.Groups["n"].Value.StartsWith('0')))
            {
                end = d; last = n; width = digits; continue;
            }
            Flush();
            start = end = d; prefix = p; last = n; width = digits;
        }
        Flush();
        return string.Join(", ", parts);

        void Flush()
        {
            if (start is null) return;
            parts.Add(start == end ? start : $"{start}–{end}");
            start = end = null;
        }
    }
}

/// <summary>Jednostka współrzędnych P&amp;P. W bazie: oryginał Klienta i przeliczenie na mm.</summary>
public enum CoordinateUnit { Auto, Millimeters, Mils }

public static class Coordinates
{
    public const decimal MilInMm = 0.0254m;

    /// <summary>Jednostka z nagłówka kolumny, np. „Center-X(mil)” → mils, „X (mm)” → mm.</summary>
    public static CoordinateUnit FromHeader(string? header)
    {
        var h = (header ?? "").ToLowerInvariant();
        return h.Contains("mil") ? CoordinateUnit.Mils : h.Contains("mm") ? CoordinateUnit.Millimeters : CoordinateUnit.Auto;
    }

    /// <summary>
    /// Wartość w mm: jednostka w samej wartości („120mil”, „3.05mm”) ma pierwszeństwo, potem jednostka wybrana dla pliku;
    /// „Auto” bez jednostki w wartości = mm. Przecinek dziesiętny dozwolony. Nieczytelna wartość → null (bez zgadywania).
    /// </summary>
    public static decimal? ToMillimeters(string? text, CoordinateUnit unit)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var t = text.Trim().ToLowerInvariant().Replace(" ", "");
        var valueUnit = t.EndsWith("mil") ? CoordinateUnit.Mils : t.EndsWith("mm") ? CoordinateUnit.Millimeters : unit;
        t = t.Replace("mil", "").Replace("mm", "").Replace(',', '.');
        if (!decimal.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) return null;
        return Math.Round(valueUnit == CoordinateUnit.Mils ? number * MilInMm : number, 4);
    }
}

// ───────────── Decyzje na linii, MPN, wersje ─────────────

/// <summary>Decyzja Opiekuna po porównaniu (stałe kody).</summary>
public static class ComparisonDecisions
{
    public const string Checked = "SPRAWDZONE";
    public const string DifferenceList = "WYKAZ_ROZNIC";
}

/// <summary>MPN przypięty do linii: zatwierdzony (z podstawą) albo niedozwolony (z powodem). Usuwanie tylko z powodem.</summary>
public sealed class LineMpn
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BomLineId { get; set; }
    public string Mpn { get; set; } = "";
    public bool Forbidden { get; set; }
    /// <summary>Podstawa (zatwierdzony) albo powód (niedozwolony).</summary>
    public string Note { get; set; } = "";
    public int Ordinal { get; set; }
    public Guid? AddedByUserId { get; set; }
    public string AddedBy { get; set; } = "";
    public DateTimeOffset AddedAtUtc { get; set; }
}

public static class MpnBases
{
    public const string FromCustomerBom = "z BOM Klienta";
    public const string FromCustomerBomPreferred = "z BOM Klienta (preferowany)";

    /// <summary>Kilka MPN w komórce (2.3): separator „;”, „|” albo nowy wiersz; oryginał zostaje w sekcji Oryginał.</summary>
    public static IReadOnlyList<string> Split(string? partNumber) =>
        (partNumber ?? "").Split([';', '|', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal).ToList();
}

/// <summary>Zatwierdzona wersja BOM — niezmienna, pełna kopia linii (ADR-0007, 10.4).</summary>
public sealed class BomVersion
{
    public const string Base = "BAZOWA";
    public const string ForOrder = "DLA_ZK";

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PackageId { get; set; }
    public int Number { get; set; }
    public Guid BomRevisionId { get; set; }
    public string Kind { get; set; } = Base;
    /// <summary>Numer ZK w formacie nnn/rrrr — dla wersji dla ZK.</summary>
    public string? OrderNumber { get; set; }
    public string? Comment { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string ApprovedBy { get; set; } = "";
    public DateTimeOffset ApprovedAtUtc { get; set; }
    /// <summary>Kopia linii w chwili zatwierdzenia (JSON): numer, desygnatory, ilość, Part TS, montaż, MPN, potwierdzenie.</summary>
    public string LinesJson { get; set; } = "[]";
    public int LineCount { get; set; }
    public int UnconfirmedCount { get; set; }

    public string DisplayName => $"v{Number} " + (Kind == ForOrder ? $"dla ZK {OrderNumber}" : "bazowa") + $" — {ApprovedAtUtc.ToLocalTime():dd.MM.yyyy}, {ApprovedBy}";
}

// ───────────── Komendy (każda ma swój handler) ─────────────

public interface ICommandHandler<in TCommand>
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <param name="Options">Pola wybrane do porównania (kolumna „Comparator” przy imporcie); null = bez zmiany.</param>
public sealed record AttachPnp(Guid RevisionId, string FileName, byte[] Content, IReadOnlyList<PnpRow> Rows, ComparatorOptions? Options = null);
public sealed record SetComparisonDecision(Guid LineId, string? Decision, string? Question);
// Rozstrzygnięcie linii: patrz Resolution.cs (ResolveLine).
public sealed record RecordCustomerAnswer(Guid LineId, string Answer);
public sealed record UpdateLineTs(Guid LineId, string? PartTs, string? MountingCode, string? Comment);
public sealed record AddLineMpn(Guid LineId, string Mpn, bool Forbidden, string Note);
public sealed record RemoveLineMpn(Guid MpnId, string Reason);
public sealed record ApprovePnp(Guid RevisionId);
public sealed record ApproveBom(Guid RevisionId, string Kind, string? OrderNumber, string? Comment);

/// <summary>Odczyty dla ekranów procesu BOM (serwis odczytu, bez agregatów do edycji).</summary>
public interface IBomWorkflowReader
{
    /// <summary>Rewizja importu bez linii (P&amp;P, zatwierdzenie P&amp;P).</summary>
    Task<BomRevision?> RevisionAsync(Guid revisionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PnpRow>> PnpRowsAsync(Guid revisionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LineMpn>> MpnsAsync(Guid revisionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BomVersion>> VersionsAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MountingOption>> MountingMethodsAsync(CancellationToken cancellationToken = default);
}

public sealed record MountingOption(string Code, string Name);

/// <summary>Numer ZK w formacie nnn/rrrr (3.7).</summary>
public static class OrderNumbers
{
    public static bool IsValid(string? value) =>
        value is not null && System.Text.RegularExpressions.Regex.IsMatch(value.Trim(), @"^\d{1,4}/\d{4}$");

    public static string Normalize(string value) => value.Trim();
}

internal static class Invariant
{
    public static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);
}
