using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using iPart.Core.Packages;

namespace iPart.Infrastructure.Packages;

/// <summary>
/// Wykaz Różnic Dokumentacyjnych w układzie firmowego wzoru: Designator / Part TS / Footprint / Description — BOM i P&amp;P,
/// PYTANIE, ODPOWIEDŹ (K:O scalone). Klient wpisuje odpowiedzi w kolumnie ODPOWIEDŹ; odczyt dopasowuje wiersze po Designator BOM.
/// </summary>
public sealed class DifferenceListWorkbook : IDifferenceListWorkbook
{
    private static readonly string[] Headers =
    [
        "Designator\nBOM", "Designator\nP&P", "Part TS\nBOM", "Part TS\nP&P", "Footprint\nBOM", "Footprint\nP&P",
        "Description\nBOM", "Description\nP&P", "PYTANIE", "", "ODPOWIEDŹ"
    ];

    public byte[] Write(string title, IReadOnlyList<DifferenceListRow> rows)
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbook = document.AddWorkbookPart();
            workbook.Workbook = new Workbook();
            var styles = workbook.AddNewPart<WorkbookStylesPart>();
            styles.Stylesheet = Stylesheet();
            var sheetPart = workbook.AddNewPart<WorksheetPart>();
            var data = new SheetData();
            data.Append(Row(1, Headers, header: true));
            var r = 2u;
            foreach (var row in rows)
                data.Append(Row(r++, [row.DesignatorsBom, row.DesignatorsPnp, row.PartTsBom, row.PartTsPnp, row.FootprintBom, row.FootprintPnp,
                    row.DescriptionBom, row.DescriptionPnp, row.Question, null, row.Answer], header: false));
            var columns = new Columns(
                new Column { Min = 1, Max = 8, Width = 20.7109375, CustomWidth = true },
                new Column { Min = 9, Max = 9, Width = 30.7109375, CustomWidth = true },
                new Column { Min = 11, Max = 15, Width = 20.7109375, CustomWidth = true });
            var merges = new MergeCells();
            for (var i = 1u; i < r; i++)
            {
                merges.Append(new MergeCell { Reference = $"K{i}:O{i}" });
            }
            sheetPart.Worksheet = new Worksheet(columns, data, merges);
            workbook.Workbook.AppendChild(new Sheets(new Sheet { Id = workbook.GetIdOfPart(sheetPart), SheetId = 1, Name = Truncate(title, 31) }));
            workbook.Workbook.Save();
        }
        return stream.ToArray();
    }

    public IReadOnlyList<(string DesignatorsBom, string Answer)> ReadAnswers(byte[] content)
    {
        var sheet = BomWorkbookReader.Read(content).FirstOrDefault(s => s.Rows.Count > 0);
        if (sheet is null) return [];
        // Wiersz nagłówków: ten, w którym jest „ODPOWIEDŹ”; kolumna Designator BOM — pierwsza z „Designator”.
        var header = sheet.Rows.FirstOrDefault(r => r.Values.Any(v => Clean(v)?.StartsWith("ODPOWIED", StringComparison.OrdinalIgnoreCase) == true));
        if (header.Values is null) return [];
        var answerColumn = IndexOf(header.Values, v => v.StartsWith("ODPOWIED", StringComparison.OrdinalIgnoreCase));
        var designatorColumn = IndexOf(header.Values, v => v.StartsWith("Designator", StringComparison.OrdinalIgnoreCase));
        if (answerColumn < 0 || designatorColumn < 0) return [];
        return sheet.Rows.Where(r => r.RowNumber > header.RowNumber)
            .Select(r => (Designators: Cell(r.Values, designatorColumn), Answer: Cell(r.Values, answerColumn)))
            .Where(x => x.Designators is not null && x.Answer is not null)
            .Select(x => (x.Designators!, x.Answer!)).ToList();
    }

    private static int IndexOf(IReadOnlyList<string?> values, Func<string, bool> match)
    {
        for (var i = 0; i < values.Count; i++) if (Clean(values[i]) is { } v && match(v)) return i;
        return -1;
    }

    private static string? Cell(IReadOnlyList<string?> values, int index) => index < values.Count ? Clean(values[index]) : null;
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    private static Row Row(uint index, IReadOnlyList<string?> values, bool header)
    {
        var row = new Row { RowIndex = index };
        for (var i = 0; i < values.Count; i++)
        {
            var cell = new Cell { CellReference = $"{(char)('A' + i)}{index}", StyleIndex = header ? 1u : 2u };
            if (values[i] is { } text)
            {
                cell.DataType = CellValues.InlineString;
                cell.InlineString = new InlineString(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            }
            row.Append(cell);
        }
        return row;
    }

    // Styl 1: nagłówek — pogrubiony, zawijany, obramowany; styl 2: dane — zawijane, obramowane, wyrównane do góry.
    private static Stylesheet Stylesheet() => new(
        new Fonts(new Font(), new Font(new Bold())),
        new Fills(new Fill(new PatternFill { PatternType = PatternValues.None }), new Fill(new PatternFill { PatternType = PatternValues.Gray125 })),
        new Borders(new Border(), new Border(
            new LeftBorder { Style = BorderStyleValues.Thin }, new RightBorder { Style = BorderStyleValues.Thin },
            new TopBorder { Style = BorderStyleValues.Thin }, new BottomBorder { Style = BorderStyleValues.Thin }, new DiagonalBorder())),
        new CellFormats(
            new CellFormat(),
            new CellFormat { FontId = 1, BorderId = 1, ApplyFont = true, ApplyBorder = true, ApplyAlignment = true,
                Alignment = new Alignment { WrapText = true, Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center } },
            new CellFormat { BorderId = 1, ApplyBorder = true, ApplyAlignment = true,
                Alignment = new Alignment { WrapText = true, Vertical = VerticalAlignmentValues.Top } }));
}
