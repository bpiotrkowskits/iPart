using iPart.Core.Packages;
using iPart.Infrastructure.Import;

namespace iPart.Infrastructure.Packages;

public sealed record WorkbookSheetGrid(string Name, IReadOnlyList<(int RowNumber, IReadOnlyList<string?> Values)> Rows, int ColumnCount)
{
    public override string ToString() => Name;

    // Tabela od wskazanego wiersza nagłówków (numer wiersza jak w Excelu).
    public BomSourceTable ToTable(int headerRowNumber)
    {
        var header = Rows.FirstOrDefault(r => r.RowNumber == headerRowNumber);
        var headers = Enumerable.Range(0, ColumnCount).Select(i =>
        {
            var text = header.Values is not null && i < header.Values.Count ? header.Values[i]?.Trim() : null;
            return string.IsNullOrWhiteSpace(text) ? $"Kolumna {OpenXmlWorkbookReader.ColumnName(i + 1)}" : text!;
        }).ToList();
        var rows = Rows.Where(r => r.RowNumber > headerRowNumber)
            .Select(r => new BomSourceRow(r.RowNumber, r.Values)).ToList();
        return new BomSourceTable(headers, rows);
    }
}

// Odczyt skoroszytu Klienta (.xlsx) tylko do odczytu — bez Excela, makr i przeliczania formuł.
public static class BomWorkbookReader
{
    public static IReadOnlyList<WorkbookSheetGrid> Read(string path, CancellationToken cancellationToken = default)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var sheets = new OpenXmlWorkbookReader().Read(stream, null, cancellationToken);
        var result = new List<WorkbookSheetGrid>();
        foreach (var sheet in sheets)
        {
            // OpenXmlWorkbookReader wydziela wykryty nagłówek — łączymy z powrotem, bo nagłówek wybiera użytkownik.
            var cells = sheet.HeaderCells.Concat(sheet.Rows.SelectMany(r => r.Cells)).ToList();
            if (cells.Count == 0) { result.Add(new(sheet.Name, [], 0)); continue; }
            var columnCount = cells.Max(c => c.Column);
            var rows = cells.GroupBy(c => c.Row).OrderBy(g => g.Key).Select(g =>
            {
                var values = new string?[columnCount];
                foreach (var cell in g) values[cell.Column - 1] = cell.Value;
                return (g.Key, (IReadOnlyList<string?>)values);
            }).ToList();
            result.Add(new(sheet.Name, rows, columnCount));
        }
        return result;
    }
}
