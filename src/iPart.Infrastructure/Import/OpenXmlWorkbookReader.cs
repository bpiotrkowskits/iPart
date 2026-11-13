using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using iPart.Core.Import;
using System.Globalization;

namespace iPart.Infrastructure.Import;

public sealed class OpenXmlWorkbookReader
{
    public List<SourceSheet> Read(Stream input, IProgress<ImportProgress>? progress, CancellationToken cancellationToken)
    {
        using var workbook = SpreadsheetDocument.Open(input, false, new OpenSettings { AutoSave = false });
        var part = workbook.WorkbookPart ?? throw new InvalidDataException("Brak części skoroszytu w pliku XLSX.");
        var shared = new List<string>();
        if (part.SharedStringTablePart is { } strings)
        {
            using var stringReader = OpenXmlReader.Create(strings);
            while (stringReader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (stringReader.IsStartElement && stringReader.ElementType == typeof(SharedStringItem))
                    shared.Add(stringReader.LoadCurrentElement()!.InnerText);
            }
        }
        var formats = part.WorkbookStylesPart?.Stylesheet?.NumberingFormats?.Elements<NumberingFormat>()
            .Where(x => x.NumberFormatId is not null).ToDictionary(x => x.NumberFormatId!.Value, x => x.FormatCode?.Value ?? "General") ?? [];
        var cellFormats = part.WorkbookStylesPart?.Stylesheet?.CellFormats?.Elements<CellFormat>().ToArray() ?? [];
        var sheets = part.Workbook?.Sheets?.Elements<Sheet>().ToArray() ?? [];
        var result = new List<SourceSheet>();
        var rowsRead = 0;
        for (var index = 0; index < sheets.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var definition = sheets[index];
            var sheet = new SourceSheet { Name = definition.Name?.Value ?? $"Arkusz {index + 1}" };
            result.Add(sheet);
            if (definition.Id is null || part.GetPartById(definition.Id!) is not WorksheetPart worksheet) continue;
            using var reader = OpenXmlReader.Create(worksheet);
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!reader.IsStartElement || reader.ElementType != typeof(Row)) continue;
                var row = (Row)reader.LoadCurrentElement()!;
                rowsRead++;
                if (rowsRead % 250 == 0) progress?.Report(new("Odczyt XLSX", sheet.Name, rowsRead, index * 60 / Math.Max(1, sheets.Length)));
                var cells = new List<SourceCell>();
                var nextColumn = 1;
                foreach (var cell in row.Elements<Cell>())
                {
                    var column = cell.CellReference is not null ? ColumnNumber(cell.CellReference!) : nextColumn;
                    nextColumn = column + 1;
                    var address = cell.CellReference?.Value ?? ColumnName(column) + (row.RowIndex?.Value ?? (uint)rowsRead);
                    var formula = cell.CellFormula?.Text;
                    // Shared formula followers may have an empty <f/>: retain that fact too.
                    if (cell.CellFormula is not null) formula ??= "";
                    var raw = cell.CellValue?.Text;
                    string? value = raw;
                    var kind = ExcelCellKind.Number;
                    if (cell.DataType?.Value == CellValues.SharedString)
                    {
                        kind = ExcelCellKind.Text;
                        value = int.TryParse(raw, out var position) && position >= 0 && position < shared.Count ? shared[position] : null;
                    }
                    else if (cell.DataType?.Value == CellValues.InlineString) { kind = ExcelCellKind.Text; value = cell.InlineString?.InnerText; }
                    else if (cell.DataType?.Value == CellValues.String) kind = ExcelCellKind.Text;
                    else if (cell.DataType?.Value == CellValues.Boolean) kind = ExcelCellKind.Boolean;
                    else if (cell.DataType?.Value == CellValues.Error) kind = ExcelCellKind.Error;
                    var original = value ?? raw ?? "";
                    if (kind == ExcelCellKind.Number && value is not null) value = ExpandNumericExponent(value);
                    uint formatId = 0;
                    if (cell.StyleIndex is { } style && style.Value < cellFormats.Length) formatId = cellFormats[style.Value].NumberFormatId?.Value ?? 0;
                    var format = formats.GetValueOrDefault(formatId) ?? (formatId switch { 0 => "General", 1 => "0", 2 => "0.00", 9 => "0%", 10 => "0.00%", 49 => "@", _ => $"[built-in:{formatId}]" });
                    if (string.IsNullOrWhiteSpace(value) && formula is null) continue;
                    cells.Add(new SourceCell { Sheet = sheet.Name, Row = (int)(row.RowIndex?.Value ?? (uint)rowsRead), Column = column,
                        Address = address, OriginalValue = original,
                        Value = value, Formula = formula, Kind = kind, NumberFormat = format });
                }
                if (cells.Count == 0) continue;
                var knownDataSheet = sheet.Name is "partsData" or "resValue" or "capValue" or "ctceValue";
                if (knownDataSheet && cells.All(x => x.Formula is not null))
                {
                    sheet.IgnoredFormulaRows++;
                    foreach (var cell in cells) cell.Mapping = "Wiersz wyłącznie formuł — bez komponentu";
                    sheet.Rows.Add(new(sheet.Name, cells[0].Row, cells, true));
                    continue;
                }
                sheet.Rows.Add(new(sheet.Name, cells[0].Row, cells));
                if (rowsRead % 250 == 0) progress?.Report(new("Odczyt XLSX", sheet.Name, rowsRead, index * 60 / Math.Max(1, sheets.Length)));
            }
            DetectHeaders(sheet);
            progress?.Report(new("Odczyt XLSX", sheet.Name, rowsRead, (index + 1) * 60 / Math.Max(1, sheets.Length)));
        }
        return result;
    }

    private static void DetectHeaders(SourceSheet sheet)
    {
        var header = sheet.Rows.FirstOrDefault(r => !r.IsPreparedFormulaRow && r.Cells.Any(c => c.Formula is null && string.Equals(c.Value?.Trim(), "Part Number", StringComparison.OrdinalIgnoreCase)))
            ?? sheet.Rows.FirstOrDefault(r => !r.IsPreparedFormulaRow);
        if (header is null) return;
        sheet.HeaderRow = header.Number;
        foreach (var cell in header.Cells)
            if (cell.Formula is null && !string.IsNullOrWhiteSpace(cell.Value)) sheet.Headers[cell.Column] = cell.Value.Trim();
        // Rows preceding a detected header remain in source, rather than disappearing.
        sheet.HeaderCells.AddRange(header.Cells);
        foreach (var cell in sheet.HeaderCells) { cell.Header = cell.Value?.Trim(); cell.Mapping = "Nagłówek"; }
        sheet.Rows.Remove(header);
        foreach (var cell in sheet.Rows.SelectMany(x => x.Cells)) cell.Header = sheet.Headers.GetValueOrDefault(cell.Column);
    }

    public static int ColumnNumber(string address)
    {
        var result = 0;
        foreach (var c in address) { if (c is < 'A' or > 'Z') break; result = checked(result * 26 + c - 'A' + 1); }
        return result;
    }

    // Excel XML may serialize numeric cells in scientific notation. Expand digits exactly;
    // never use double or a decimal parser that could round before the existing domain parser validates.
    private static string ExpandNumericExponent(string text)
    {
        var marker = text.IndexOfAny(['e', 'E']);
        if (marker < 0 || !int.TryParse(text.AsSpan(marker + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var exponent) || Math.Abs((long)exponent) > 128) return text;
        var mantissa = text[..marker]; var sign = "";
        if (mantissa.StartsWith('-') || mantissa.StartsWith('+')) { sign = mantissa[..1]; mantissa = mantissa[1..]; }
        var point = mantissa.IndexOf('.');
        var digits = mantissa.Replace(".", "");
        if (digits.Length == 0 || digits.Any(c => c is < '0' or > '9') || mantissa.Count(c => c == '.') > 1) return text;
        var position = (point < 0 ? mantissa.Length : point) + exponent;
        return sign + (position <= 0 ? "0." + new string('0', -position) + digits : position >= digits.Length
            ? digits + new string('0', position - digits.Length) : digits.Insert(position, "."));
    }
    public static string ColumnName(int column)
    {
        var result = "";
        while (column > 0) { column--; result = (char)('A' + column % 26) + result; column /= 26; }
        return result;
    }
}
