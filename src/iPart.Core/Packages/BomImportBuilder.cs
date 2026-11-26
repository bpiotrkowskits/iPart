using System.Globalization;
using iPart.Core.Import;

namespace iPart.Core.Packages;

public enum BomField { None, Designator, Quantity, PartTs, Footprint, Description, Value, PartNumber, Notes }

public static class BomFields
{
    public static string Label(BomField field) => field switch
    {
        BomField.None => "(bez przypisania → kolumna dodatkowa)",
        BomField.Designator => "Designator",
        BomField.Quantity => "Quantity",
        BomField.PartTs => "Part TS",
        BomField.Footprint => "Footprint",
        BomField.Description => "Description",
        BomField.Value => "Value",
        BomField.PartNumber => "Part Number",
        BomField.Notes => "Notes",
        _ => field.ToString()
    };
}

// Tabela odczytana z arkusza Klienta: nagłówki i wiersze danych (wartości tekstowe, null = pusta komórka).
public sealed record BomSourceTable(IReadOnlyList<string> Headers, IReadOnlyList<BomSourceRow> Rows);
public sealed record BomSourceRow(int SourceRowNumber, IReadOnlyList<string?> Values);

// Przypisanie kolumny Klienta do pola. Kilka kolumn może trafić do jednego pola —
// wygrywa pierwsza niepusta według priorytetu (1 = najważniejsza).
public sealed record ColumnAssignment(int ColumnIndex, BomField Field, int Priority = 1);

public sealed record BomImportIssue(ImportSeverity Severity, int? LineNumber, int? SourceRow, string Message)
{
    public string SeverityLabel => Severity switch { ImportSeverity.Error => "Błąd", ImportSeverity.Warning => "Ostrzeżenie", _ => "Informacja" };
}

public sealed class BomImportResult
{
    public List<BomLine> Lines { get; } = [];
    public List<BomImportIssue> Issues { get; } = [];
    public bool HasErrors => Issues.Any(x => x.Severity == ImportSeverity.Error);
    public int DesignatorCount => Lines.Sum(x => x.ExpandedDesignators.Count);
}

public static class BomImportBuilder
{
    public static BomImportResult Build(BomSourceTable table, IReadOnlyList<ColumnAssignment> assignments)
    {
        var result = new BomImportResult();
        var byField = assignments.Where(a => a.Field != BomField.None)
            .GroupBy(a => a.Field)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.Priority).ThenBy(a => a.ColumnIndex).Select(a => a.ColumnIndex).ToArray());
        var mapped = byField.Values.SelectMany(x => x).ToHashSet();
        var extraColumns = Enumerable.Range(0, table.Headers.Count).Where(i => !mapped.Contains(i)).ToArray();

        if (!byField.ContainsKey(BomField.Designator))
            result.Issues.Add(new(ImportSeverity.Error, null, null, "Żadna kolumna nie jest przypisana do pola Designator."));
        if (!byField.ContainsKey(BomField.PartNumber) && !byField.ContainsKey(BomField.Value) && !byField.ContainsKey(BomField.PartTs))
            result.Issues.Add(new(ImportSeverity.Warning, null, null, "Brak kolumny Part Number, Value ani Part TS — linie nie będą mówiły, co montować."));
        foreach (var duplicatePriority in assignments.Where(a => a.Field != BomField.None)
                     .GroupBy(a => (a.Field, a.Priority)).Where(g => g.Count() > 1))
            result.Issues.Add(new(ImportSeverity.Error, null, null,
                $"Pole {BomFields.Label(duplicatePriority.Key.Field)}: kilka kolumn ma ten sam priorytet {duplicatePriority.Key.Priority}. Ustal, która kolumna jest nadrzędna."));

        var lineNumber = 0;
        foreach (var row in table.Rows)
        {
            if (row.Values.All(string.IsNullOrWhiteSpace)) continue;
            var line = new BomLine { LineNumber = ++lineNumber };
            line.Designators = Pick(row, byField, BomField.Designator);
            line.PartTs = Pick(row, byField, BomField.PartTs);
            line.Footprint = Pick(row, byField, BomField.Footprint);
            line.Description = Pick(row, byField, BomField.Description);
            line.Value = Pick(row, byField, BomField.Value);
            line.PartNumber = Pick(row, byField, BomField.PartNumber);
            line.Notes = Pick(row, byField, BomField.Notes);

            var quantityText = Pick(row, byField, BomField.Quantity);
            if (quantityText is not null)
            {
                if (decimal.TryParse(quantityText.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var q)
                    && q >= 0 && q == decimal.Truncate(q) && q <= int.MaxValue)
                    line.Quantity = (int)q;
                else
                    result.Issues.Add(new(ImportSeverity.Error, line.LineNumber, row.SourceRowNumber, $"Quantity „{quantityText}” nie jest nieujemną liczbą całkowitą."));
            }

            var parsed = DesignatorParser.Parse(line.Designators);
            foreach (var designator in parsed.Designators)
                line.ExpandedDesignators.Add(new BomLineDesignator { BomLineId = line.Id, Designator = designator });
            foreach (var note in parsed.Notes)
                result.Issues.Add(new(note.Severity, line.LineNumber, row.SourceRowNumber, note.Message));

            if (line.Designators is null)
                result.Issues.Add(new(ImportSeverity.Warning, line.LineNumber, row.SourceRowNumber, "Linia bez desygnatorów."));
            else if (line.Quantity is { } quantity && quantity != parsed.Designators.Count)
                result.Issues.Add(new(ImportSeverity.Warning, line.LineNumber, row.SourceRowNumber,
                    $"Quantity = {quantity}, a desygnatorów jest {parsed.Designators.Count}."));

            var ordinal = 0;
            foreach (var column in extraColumns)
            {
                ordinal++;
                var value = Cell(row, column);
                if (value is null) continue;
                line.ExtraValues.Add(new BomLineExtraValue { BomLineId = line.Id, Ordinal = ordinal, Header = table.Headers[column], Value = value });
            }
            result.Lines.Add(line);
        }

        foreach (var duplicate in result.Lines.SelectMany(l => l.ExpandedDesignators.Select(d => (d.Designator, l.LineNumber)))
                     .GroupBy(x => x.Designator, StringComparer.Ordinal)
                     .Where(g => g.Select(x => x.LineNumber).Distinct().Count() > 1))
            result.Issues.Add(new(ImportSeverity.Error, null, null,
                $"Desygnator {duplicate.Key} występuje w kilku liniach: {string.Join(", ", duplicate.Select(x => x.LineNumber).Distinct())}."));

        if (result.Lines.Count == 0)
            result.Issues.Add(new(ImportSeverity.Error, null, null, "Brak linii z danymi pod wierszem nagłówków."));
        return result;
    }

    private static string? Pick(BomSourceRow row, Dictionary<BomField, int[]> byField, BomField field)
    {
        if (!byField.TryGetValue(field, out var columns)) return null;
        foreach (var column in columns)
            if (Cell(row, column) is { } value) return value;
        return null;
    }

    private static string? Cell(BomSourceRow row, int column) =>
        column < row.Values.Count && !string.IsNullOrWhiteSpace(row.Values[column]) ? row.Values[column]!.Trim() : null;
}
