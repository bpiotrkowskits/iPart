using System.Globalization;
using iPart.Core.Import;
using ProduMan.SharedKernel.Common;

namespace iPart.Core.Packages;

// ───────────── P&P ─────────────

public enum PnpField { None, Designator, X, Y, Rotation, Side, Footprint, Value }

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
        PnpField.Value => "Value / Comment",
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
    public string? Rotation { get; set; }
    public string? Side { get; set; }
    public string? Footprint { get; set; }
    public string? Value { get; set; }
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
    public static PnpImportResult Build(BomSourceTable table, IReadOnlyList<PnpColumnAssignment> assignments)
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
                Side = Cell(row, PnpField.Side), Footprint = Cell(row, PnpField.Footprint), Value = Cell(row, PnpField.Value),
            });
        }
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
    public static IReadOnlyList<(int RowNumber, IReadOnlyList<string?> Values)> Read(string text)
    {
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var sample = lines.Where(l => !string.IsNullOrWhiteSpace(l)).Take(50).ToList();
        char? separator = Count('\t') > 0 ? '\t' : Count(';') >= sample.Count / 2 && Count(';') > 0 ? ';' : Count(',') >= sample.Count / 2 && Count(',') > 0 ? ',' : null;
        var rows = new List<(int, IReadOnlyList<string?>)>();
        for (var i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var cells = separator is { } s ? SplitQuoted(lines[i], s) : lines[i].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
            rows.Add((i + 1, cells.Select(c => (string?)c.Trim().Trim('"')).ToList()));
        }
        return rows;

        int Count(char c) => sample.Sum(l => l.Count(x => x == c));
    }

    private static List<string> SplitQuoted(string line, char separator)
    {
        var cells = new List<string>();
        var current = new System.Text.StringBuilder();
        var quoted = false;
        foreach (var ch in line)
        {
            if (ch == '"') { quoted = !quoted; continue; }
            if (ch == separator && !quoted) { cells.Add(current.ToString()); current.Clear(); continue; }
            current.Append(ch);
        }
        cells.Add(current.ToString());
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

public sealed record LineComparison(Guid LineId, int LineNumber, IReadOnlyList<LineDifference> Differences)
{
    public bool HasDifferences => Differences.Count > 0;
}

public sealed record BomPnpComparison(IReadOnlyList<LineComparison> Lines, IReadOnlyList<string> OnlyInPnp)
{
    public int Differences => Lines.Count(l => l.Differences.Any(d => d.Severity == DifferenceSeverity.Difference));
    public int Warnings => Lines.Count(l => l.Differences.Count > 0 && l.Differences.All(d => d.Severity == DifferenceSeverity.Warning));
}

/// <summary>
/// Porównanie BOM z P&amp;P na poziomie desygnatora (1.3): brak w P&amp;P, Footprint, Value; brak danych po obu stronach
/// to ostrzeżenie, nie rozbieżność; Quantity różne od liczby desygnatorów — ostrzeżenie.
/// Normalizacja tylko do porównania (bez wielkości liter i spacji, µ = μ) — dane zostają oryginalne (1.2).
/// </summary>
public static class BomPnpComparer
{
    public static BomPnpComparison Compare(IReadOnlyList<BomLine> lines, IReadOnlyList<PnpRow> pnp)
    {
        var byDesignator = pnp.GroupBy(p => Normalize(p.Designator)).ToDictionary(g => g.Key, g => g.First());
        var inBom = new HashSet<string>();
        var result = new List<LineComparison>();
        foreach (var line in lines.OrderBy(l => l.LineNumber))
        {
            var differences = new List<LineDifference>();
            var designators = line.ExpandedDesignators.Select(d => d.Designator).ToList();
            foreach (var d in designators) inBom.Add(Normalize(d));

            var missing = designators.Where(d => !byDesignator.ContainsKey(Normalize(d))).ToList();
            if (missing.Count > 0 && pnp.Count > 0)
                differences.Add(new(DifferenceSeverity.Difference, "Brak w P&P", $"Desygnatory z BOM, których nie ma w P&P: {Join(missing)}."));

            var matched = designators.Where(d => byDesignator.ContainsKey(Normalize(d))).Select(d => byDesignator[Normalize(d)]).ToList();
            var bomFootprint = line.Footprint;
            var pnpFootprints = matched.Where(p => !string.IsNullOrWhiteSpace(p.Footprint)).ToList();
            if (string.IsNullOrWhiteSpace(bomFootprint) && matched.Count > 0 && pnpFootprints.Count == 0)
                differences.Add(new(DifferenceSeverity.Warning, "Brak danych po obu stronach", "Brak Footprint w BOM i w P&P — ostrzeżenie, nie rozbieżność."));
            else if (!string.IsNullOrWhiteSpace(bomFootprint))
            {
                var wrong = pnpFootprints.Where(p => Normalize(p.Footprint) != Normalize(bomFootprint)).ToList();
                if (wrong.Count > 0)
                    differences.Add(new(DifferenceSeverity.Difference, "Footprint",
                        $"BOM: {bomFootprint}; P&P: {Join(wrong.Select(p => $"{p.Designator} = {p.Footprint}"))}."));
            }

            if (!string.IsNullOrWhiteSpace(line.Value))
            {
                var wrongValue = matched.Where(p => !string.IsNullOrWhiteSpace(p.Value) && Normalize(p.Value) != Normalize(line.Value)).ToList();
                if (wrongValue.Count > 0)
                    differences.Add(new(DifferenceSeverity.Difference, "Value",
                        $"BOM: {line.Value}; P&P: {Join(wrongValue.Select(p => $"{p.Designator} = {p.Value}"))}."));
            }

            if (line.Quantity is { } qty && qty != designators.Count && designators.Count > 0)
                differences.Add(new(DifferenceSeverity.Warning, "Ilość ≠ desygnatory", $"Quantity = {qty}, desygnatorów = {designators.Count}."));

            result.Add(new(line.Id, line.LineNumber, differences));
        }
        var onlyInPnp = pnp.Where(p => !inBom.Contains(Normalize(p.Designator))).Select(p => p.Designator).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        return new(result, onlyInPnp);
    }

    /// <summary>Tylko do porównania: bez spacji i wielkości liter, µ (mikro) = μ (mu).</summary>
    public static string Normalize(string? value) =>
        string.Concat((value ?? "").Where(c => !char.IsWhiteSpace(c))).Replace('µ', 'μ').ToUpperInvariant();

    private static string Join(IEnumerable<string> items)
    {
        var list = items.ToList();
        return list.Count <= 12 ? string.Join(", ", list) : string.Join(", ", list.Take(12)) + $" … (+{list.Count - 12})";
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

public sealed record AttachPnp(Guid RevisionId, string FileName, byte[] Content, IReadOnlyList<PnpRow> Rows);
public sealed record SetComparisonDecision(Guid LineId, string? Decision, string? Question);
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
