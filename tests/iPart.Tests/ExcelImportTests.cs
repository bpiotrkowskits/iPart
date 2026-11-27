using System.Security.Cryptography;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using iPart.Core.Import;
using iPart.Infrastructure.Import;

namespace iPart.Tests;

public sealed class ExcelImportTests
{
    internal static ImportReferences References => new(new Dictionary<string, int>(StringComparer.Ordinal) { ["SMT"] = 1, ["Mech"] = 6 },
        new Dictionary<string, int>(StringComparer.Ordinal) { ["AEC-Q200"] = 1 }, new HashSet<ImportBusinessKey>());
    internal sealed class FakeReferences(ImportReferences? references = null) : IImportReferenceReader
    {
        public Task<ImportReferences> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(references ?? References);
    }
    private static ExcelPreviewService Service(ImportReferences? references = null) => new(new FakeReferences(references));

    [Fact]
    public async Task ReorderedHeadersAndPreambleMapWithoutFixedColumnNumbers()
    {
        using var file = new WorkbookFile(new SheetSpec("partsData", [Row("Title"), Row(), Row("Notes", "Critical", "Mounting", "Part Number", "Footprint", "Case"), Row("note", false, "MECH", " 001 A (Maker) ", "FP", "CASE")]));
        var report = await Service().AnalyzeAsync(file.Path, new());
        var record = Assert.Single(report.Records);
        Assert.Equal("001 A (Maker)", record.Mpn);
        Assert.Null(record.Component.ManufacturerId);
        Assert.Equal("Mech", record.Mounting);
        Assert.False(record.Component.Critical);
        Assert.Equal("CASE", record.Component.CaseName);
        Assert.Equal("FP", record.Component.Footprint);
        Assert.Equal("D4", Assert.Single(record.FieldSources["Mpn"]).Address);
        Assert.Equal(" 001 A (Maker) ", record.FieldSources["Mpn"][0].OriginalValue);
    }

    [Fact]
    public async Task MissingAndDuplicatedRequiredHeadersAreExplicitErrors()
    {
        using var missing = new WorkbookFile(new SheetSpec("partsData", [Row("Other"), Row("data")]));
        var report = await Service().AnalyzeAsync(missing.Path, new());
        Assert.Empty(report.Records);
        Assert.Contains(report.Issues, i => i.Code == "MissingHeader" && i.Severity == ImportSeverity.Error);
        Assert.Contains(report.AllSourceCells, c => c.Value == "data");
        using var duplicate = new WorkbookFile(new SheetSpec("partsData", [Row("Part Number", "Notes", "Notes"), Row("A", "one", "two")]));
        report = await Service().AnalyzeAsync(duplicate.Path, new());
        Assert.Contains(report.Issues, i => i.Code == "DuplicateHeader");
        Assert.True(Assert.Single(report.Records).HasErrors);
        Assert.Null(report.Records[0].Component.Notes);
    }

    [Fact]
    public async Task FormulaOnlyRowsAreNotComponentsAndMissingValueCacheDoesNotStopParameters()
    {
        using var file = new WorkbookFile(new SheetSpec("partsData", [Row("Part Number", "Value"), Row("R1", new Formula("External!A1")), Row(null, new Formula("IF(A3=\"\",\"\",1)")), Row()]),
            new SheetSpec("resValue", [Row("Part Number", "Rezystancja", "Tolerancja", "Moc", "AEC-Q200", "ppm"), Row("R1", "10M", "1%", ".25W", "AEC-Q200", 100)]));
        var report = await Service().AnalyzeAsync(file.Path, new());
        var record = Assert.Single(report.Records);
        Assert.Equal("10M_1%_250mW_AEC-Q200_100ppm", record.GeneratedValue);
        Assert.False(record.HasErrors);
        Assert.Equal(1, report.Sheets[0].IgnoredFormulaRows);
        Assert.Contains(report.AllSourceCells, c => c.Address == "B3" && c.Formula is not null);
        Assert.Contains(record.Issues, i => i.Code == "FormulaWithoutCache");
        Assert.Contains(record.Sources, c => c.OriginalValue == "=External!A1" && c.Value is null);
    }

    [Fact]
    public async Task ParameterFormulaWithoutCacheIsReportedWithoutDroppingOtherFields()
    {
        using var file = new WorkbookFile(new SheetSpec("partsData", [Row("Part Number"), Row("C")]),
            new SheetSpec("capValue", [Row("Part Number", "Pojemność", "Tolerancja"), Row("C", "100nF", new Formula("1+1", Text: true))]));
        var report = await Service().AnalyzeAsync(file.Path, new());
        var row = Assert.Single(report.Records);
        Assert.Equal(100000m, row.Component.Capacitor!.CapacitancePf);
        Assert.True(row.HasErrors);
        Assert.Contains(row.Issues, x => x.Code == "FormulaWithoutCache");
    }

    [Fact]
    public async Task NumericMpnUsesZeroMaskAndFlagsUnrecoverablePrecision()
    {
        using var file = new WorkbookFile(new SheetSpec("partsData", [Row("Part Number"), Row(new Number("123", 1)), Row(" 000123 "), Row(new Number("1234567890123456")), Row(new Number("42"))]));
        var report = await Service().AnalyzeAsync(file.Path, new());
        Assert.Equal("000123", report.Records[0].Mpn);
        Assert.Equal(1, report.DuplicateGroups);
        Assert.True(report.Records[0].HasErrors);
        Assert.True(report.Records[1].HasErrors);
        Assert.Contains(report.Records[2].Issues, x => x.Code == "UncertainMpn");
        Assert.Contains(report.Records[3].Issues, x => x.Code == "NumericMpn" && x.Severity == ImportSeverity.Warning);
        Assert.Equal("123", report.Records[0].FieldSources["Mpn"][0].OriginalValue);
    }

    [Fact]
    public async Task ExcelNumericExponentIsExpandedExactlyBeforeDomainValidation()
    {
        using var file = new WorkbookFile(new SheetSpec("partsData", [Row("Part Number"), Row(new Number("1.23E2", 1))]),
            new SheetSpec("capValue", [Row("Part Number", "Pojemność"), Row("000123", new Number("1E-9"))]));
        var report = await Service().AnalyzeAsync(file.Path, new());
        var row = Assert.Single(report.Records);
        Assert.Equal("000123", row.Mpn);
        Assert.Equal(.000000001m, row.Component.Capacitor!.CapacitancePf);
        Assert.Equal("1E-9", row.FieldSources["Capacitor.CapacitancePf"][0].OriginalValue);
    }

    [Fact]
    public async Task EveryDuplicateIsMarkedAndNoAmbiguousParameterIsAllocated()
    {
        using var file = new WorkbookFile(new SheetSpec("partsData", [Row("Part Number"), Row("X"), Row(" X "), Row("x")]),
            new SheetSpec("resValue", [Row("Part Number", "Rezystancja"), Row("X", "1k"), Row("orphan", "2k")]));
        var references = References with { ExistingKeys = new HashSet<ImportBusinessKey> { new("x", null), new("X", 5) } };
        var report = await Service(references).AnalyzeAsync(file.Path, new());
        Assert.Equal(3, report.PartsDataRecords);
        Assert.Equal(1, report.DuplicateGroups);
        Assert.All(report.Records.Take(2), r => { Assert.True(r.HasErrors); Assert.Contains(r.Issues, i => i.Code == "AmbiguousJoin"); Assert.Null(r.Component.Resistor); });
        Assert.Equal(1, report.DatabaseCollisions);
        Assert.Contains(report.Records[2].Issues, i => i.Code == "DatabaseCollision");
        Assert.Equal(2, report.UnmatchedAuxiliaryRows);
    }

    [Fact]
    public async Task ConflictsHaveNoWinningValueAndIdenticalValuesKeepAllOrigins()
    {
        using var file = new WorkbookFile(new SheetSpec("partsData", [Row("Part Number"), Row("R")]),
            new SheetSpec("resValue", [Row("Part Number", "Rezystancja", "Moc"), Row("R", "1k", ".25W"), Row("R", "2k", "250mW")]));
        var report = await Service().AnalyzeAsync(file.Path, new());
        var row = Assert.Single(report.Records);
        Assert.Null(row.Component.Resistor!.ResistanceOhms);
        Assert.Equal(.25m, row.Component.Resistor.PowerWatts);
        Assert.Contains(row.Issues, x => x.Code == "ParameterConflict");
        Assert.Contains(row.Issues, x => x.Code == "RepeatedParameter");
        Assert.Equal(2, row.FieldSources["Resistor.PowerWatts"].Count);
    }

    [Fact]
    public async Task CapacitorSheetsMergeCompatibleFieldsInBaseUnitsWithEsrInMilliohms()
    {
        using var file = new WorkbookFile(new SheetSpec("partsData", [Row("Part Number"), Row("C")]),
            new SheetSpec("capValue", [Row("Part Number", "Pojemność", "Napięcie", "Tolerancja", "Dielektryk"), Row("C", "100nF", "10V", ".5pF", "C0G")]),
            new SheetSpec("ctceValue", [Row("Part Number", "Pojemność", "ESR [mR]", "lowESR", "Life (hs)"), Row("C", "0,1μF", "ESR0025", "lowESR", 1000)]));
        var report = await Service().AnalyzeAsync(file.Path, new());
        var row = Assert.Single(report.Records);
        Assert.False(row.HasErrors);
        Assert.Equal(100000, row.Component.Capacitor!.CapacitancePf);
        Assert.Equal(.5m, row.Component.Capacitor.AbsoluteTolerancePf);
        Assert.Null(row.Component.Capacitor.TolerancePercent);
        Assert.Equal(25, row.Component.Capacitor.EsrMilliOhms);
        Assert.True(row.Component.Capacitor.LowEsr);
        Assert.Equal(1000, row.Component.Capacitor.LifetimeHours);
        Assert.Equal(2, row.FieldSources["Capacitor.CapacitancePf"].Count);
        Assert.Null(row.GeneratedValue);
    }

    [Fact]
    public async Task AlternativeToleranceConflictAndInvalidUnitsAndPrecisionBlockRecords()
    {
        using var file = new WorkbookFile(new SheetSpec("partsData", [Row("Part Number"), Row("C"), Row("BAD")]),
            new SheetSpec("capValue", [Row("Part Number", "Pojemność", "Tolerancja"), Row("C", "1uF", "1%"), Row("BAD", "1MF", "1%")]),
            new SheetSpec("ctceValue", [Row("Part Number", "Pojemność", "Tolerancja", "ESR [mR]"), Row("C", "1000000pF", "1pF", "0.0000000001")]));
        var report = await Service().AnalyzeAsync(file.Path, new());
        Assert.Equal(2, report.BlockedRecords);
        Assert.Contains(report.Records[0].Issues, x => x.Code == "ModelValidation");
        Assert.Contains(report.Records[0].Issues, x => x.Code == "InvalidParameter");
        Assert.Contains(report.Records[1].Issues, x => x.Code == "InvalidParameter");
    }

    [Fact]
    public async Task PadsPreserveNullZeroFalseAndLegacyNamesRequireExplicitDecisions()
    {
        using var file = new WorkbookFile(new SheetSpec("partsData", [Row("Part Number", "RC", "TT", "BTC", "NS", "ThP", "NonS"), Row("A", null, 0, false, null, 2, false)]));
        var report = await Service().AnalyzeAsync(file.Path, new());
        var row = Assert.Single(report.Records);
        Assert.Null(row.Component.Pads!.RC);
        Assert.Equal(0, row.Component.Pads.TT);
        Assert.False(row.Component.Pads.BTC);
        Assert.Null(row.Component.Pads.TP);
        Assert.Null(row.Component.Pads.NS);
        Assert.Contains(row.Issues, i => i.Code == "MappingConfirmationRequired");
        report = await Service().AnalyzeAsync(file.Path, new(true, true));
        Assert.Equal(2, report.Records[0].Component.Pads!.TP);
        Assert.False(report.Records[0].Component.Pads!.NS);
        Assert.DoesNotContain(report.Records[0].Issues, i => i.Code == "MappingConfirmationRequired");
    }

    [Fact]
    public async Task UnknownMountingAndBadPadsAreBlockingAndNeverCreateDictionaries()
    {
        using var file = new WorkbookFile(new SheetSpec("partsData", [Row("Part Number", "Mounting", "RC"), Row("A", "magic", 1)]));
        var report = await Service().AnalyzeAsync(file.Path, new());
        Assert.Equal(1, report.BlockedRecords);
        Assert.Contains(report.Issues, i => i.Code == "UnknownMounting");
        Assert.Contains(report.Issues, i => i.Code == "InvalidParameter");
    }

    [Fact]
    public async Task HistoricalUnnamedAndUnsupportedDataRemainVisibleAndExportable()
    {
        using var file = new WorkbookFile(new SheetSpec("partsData", [Row("Part Number", "Case confirmation", "rmFootprint", "Tag1", null), Row("A", "unknown author/date", "oldfp", "tag", "trailing")]),
            new SheetSpec("RMCase_source", [Row("Part Number", "rmFootprint"), Row("A", "RM")]), new SheetSpec("standardCase", [Row(null, "L"), Row("pattern", "name")]),
            new SheetSpec("resValue", [Row("Part Number", "Rezystancja", "Temp"), Row("A", "1R", "unknown temp syntax")]));
        var bytesBefore = File.ReadAllBytes(file.Path);
        var report = await Service().AnalyzeAsync(file.Path, new());
        Assert.Contains(report.AllSourceCells, x => x.Value == "trailing" && x.Header is null);
        Assert.Contains(report.AllSourceCells, x => x.Value == "unknown temp syntax" && x.Mapping is null);
        Assert.Contains(report.Sheets, x => x.Name == "RMCase_source");
        Assert.Contains(report.Sheets, x => x.Name == "standardCase");
        Assert.Equal(bytesBefore, File.ReadAllBytes(file.Path));
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytesBefore)), report.InputSha256);
        var jsonPath = file.Path + ".json";
        var csvPath = file.Path + ".csv";
        try
        {
            await ImportReportExporter.ExportAsync(report, jsonPath);
            await ImportReportExporter.ExportAsync(report, csvPath);
            Assert.Contains("trailing", File.ReadAllText(jsonPath));
            Assert.Contains("unknown temp syntax", File.ReadAllText(csvPath));
            await Assert.ThrowsAsync<IOException>(() => ImportReportExporter.ExportAsync(report, file.Path));
            Assert.Equal(bytesBefore, File.ReadAllBytes(file.Path));
        }
        finally { File.Delete(jsonPath); File.Delete(csvPath); }
    }

    [Theory]
    [InlineData("=CMD()")][InlineData(" +1")][InlineData("-2")][InlineData("@SUM(A1)")][InlineData("\tvalue")][InlineData("\n=1")]
    public void CsvTextCannotBecomeAFormula(string input) => Assert.StartsWith("\"'", ImportReportExporter.Csv(input));

    [Fact]
    public async Task CancellationDuringReadPublishesNoPartialReport()
    {
        var rows = new List<object?[]> { Row("Part Number", "Value") };
        for (var i = 0; i < 2000; i++) rows.Add(Row("R" + i, new Formula("1+1")));
        using var file = new WorkbookFile(new SheetSpec("partsData", rows.ToArray()));
        using var cancellation = new CancellationTokenSource();
        var progress = new InlineProgress(p => { if (p.RowsRead >= 250) cancellation.Cancel(); });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service().AnalyzeAsync(file.Path, new(), progress, cancellation.Token));
        Assert.True(cancellation.IsCancellationRequested);
    }

    [Fact]
    public async Task ControlledFullSizeWorkbookKeepsAll24000RecordsAndTheirProvenance()
    {
        var parts = new List<object?[]> { Row("Part Number", "Mounting") };
        var resistor = new List<object?[]> { Row("Part Number", "Rezystancja", "Moc", "ppm") };
        for (var i = 0; i < 24000; i++)
        {
            var mpn = "R-" + i.ToString("D6");
            parts.Add(Row(mpn, "SMT")); resistor.Add(Row(mpn, "4k7", "250mW", 100));
        }
        using var file = new WorkbookFile(new SheetSpec("partsData", parts.ToArray()), new SheetSpec("resValue", resistor.ToArray()));
        var report = await Service().AnalyzeAsync(file.Path, new());
        Assert.Equal(24000, report.PartsDataRecords);
        Assert.Equal(24000, report.ReadyRecords);
        Assert.Equal(0, report.BlockedRecords);
        Assert.Equal("4.7k_250mW_100ppm", report.Records[23999].GeneratedValue);
        Assert.Equal("resValue!B24001", report.Records[23999].FieldSources["Resistor.ResistanceOhms"][0].Location);
    }

    [Fact]
    public async Task DatabaseReadFailureDoesNotPreventSourcePreviewOrClaimReadiness()
    {
        using var file = new WorkbookFile(new SheetSpec("partsData", [Row("Part Number"), Row("A")]));
        var report = await new ExcelPreviewService(new FailingReferences()).AnalyzeAsync(file.Path, new());
        Assert.Single(report.Records);
        Assert.Equal(0, report.ReadyRecords);
        Assert.Contains(report.Issues, i => i.Code == "DatabaseUnavailable");
    }
    private sealed class FailingReferences : IImportReferenceReader
    { public Task<ImportReferences> ReadAsync(CancellationToken cancellationToken) => throw new InvalidOperationException(); }
    private sealed class InlineProgress(Action<ImportProgress> action) : IProgress<ImportProgress>
    { public void Report(ImportProgress value) => action(value); }

    internal static object?[] Row(params object?[] values) => values;
    internal sealed record SheetSpec(string Name, object?[][] Rows);
    internal sealed record Formula(string Expression, string? Cache = null, bool Text = false);
    internal sealed record Number(string Value, uint Style = 0);
    internal sealed class WorkbookFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iPart-import-" + Guid.NewGuid().ToString("N") + ".xlsx");
        public WorkbookFile(params SheetSpec[] sheets)
        {
            using var document = SpreadsheetDocument.Create(Path, SpreadsheetDocumentType.Workbook);
            var part = document.AddWorkbookPart(); part.Workbook = new Workbook();
            var styles = part.AddNewPart<WorkbookStylesPart>();
            styles.Stylesheet = new Stylesheet(new NumberingFormats(new NumberingFormat { NumberFormatId = 164, FormatCode = "000000" }),
                new CellFormats(new CellFormat(), new CellFormat { NumberFormatId = 164, ApplyNumberFormat = true }));
            var definitions = part.Workbook.AppendChild(new Sheets()); uint sheetId = 0;
            foreach (var sheet in sheets)
            {
                var worksheet = part.AddNewPart<WorksheetPart>(); var data = new SheetData(); worksheet.Worksheet = new Worksheet(data);
                for (var r = 0; r < sheet.Rows.Length; r++)
                {
                    var row = new Row { RowIndex = (uint)r + 1 }; data.Append(row);
                    for (var c = 0; c < sheet.Rows[r].Length; c++)
                    {
                        var value = sheet.Rows[r][c]; if (value is null) continue;
                        var cell = new Cell { CellReference = OpenXmlWorkbookReader.ColumnName(c + 1) + (r + 1) };
                        switch (value)
                        {
                            case Formula formula:
                                cell.CellFormula = new CellFormula(formula.Expression);
                                if (formula.Text) cell.DataType = CellValues.String;
                                if (formula.Cache is not null) cell.CellValue = new CellValue(formula.Cache);
                                break;
                            case Number number: cell.CellValue = new CellValue(number.Value); cell.StyleIndex = number.Style; break;
                            case bool flag: cell.DataType = CellValues.Boolean; cell.CellValue = new CellValue(flag ? "1" : "0"); break;
                            case int number: cell.CellValue = new CellValue(number); break;
                            default: cell.DataType = CellValues.InlineString; cell.InlineString = new InlineString(new Text(value.ToString()!) { Space = SpaceProcessingModeValues.Preserve }); break;
                        }
                        row.Append(cell);
                    }
                }
                definitions.Append(new Sheet { Id = part.GetIdOfPart(worksheet), SheetId = ++sheetId, Name = sheet.Name });
            }
        }
        public void Dispose() => File.Delete(Path);
    }
}
