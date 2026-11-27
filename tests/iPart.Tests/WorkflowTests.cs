using iPart.Core.Import;
using iPart.Core.Packages;

namespace iPart.Tests;

public sealed class WorkflowTests
{
    private static BomLine Line(int number, string designators, int? qty = null, string? footprint = null, string? value = null)
    {
        var line = new BomLine { LineNumber = number, Designators = designators, Quantity = qty, Footprint = footprint, Value = value };
        line.ExpandedDesignators = DesignatorParser.Parse(designators).Designators.Select(d => new BomLineDesignator { BomLineId = line.Id, Designator = d }).ToList();
        return line;
    }

    private static PnpRow Pnp(string designator, string? footprint = null, string? value = null) => new() { Designator = designator, Footprint = footprint, Value = value };

    [Fact]
    public void ComparisonFindsMissingFootprintValueQuantityAndOnlyInPnp()
    {
        var lines = new[]
        {
            Line(1, "R1-R3", 3, "0402", "10k"),
            Line(2, "C2A, C2B, C2C", 3, "0402"),
            Line(3, "J4", 1),
            Line(4, "TP1", 1),
            Line(5, "D3, D4, D5", 2),
        };
        var pnp = new[]
        {
            Pnp("R1", "0402", "10K"), Pnp("R2", " 0402 ", "10k"), Pnp("R3", "0402", "10 k"),
            Pnp("C2A", "0402"), Pnp("C2B", "0402"), Pnp("C2C", "0603"),
            Pnp("TP1"), Pnp("D3"), Pnp("D4"), Pnp("D5"), Pnp("U99", "QFN"),
        };
        var result = BomPnpComparer.Compare(lines, pnp);

        Assert.Empty(result.Lines[0].Differences);                                  // normalizacja tylko do porównania
        Assert.Contains(result.Lines[1].Differences, d => d.Kind == "Footprint" && d.Detail.Contains("C2C = 0603"));
        Assert.Contains(result.Lines[2].Differences, d => d.Kind == "Brak w P&P" && d.Detail.Contains("J4"));
        var tp = Assert.Single(result.Lines[3].Differences);
        Assert.Equal((DifferenceSeverity.Warning, "Brak danych po obu stronach"), (tp.Severity, tp.Kind));
        Assert.Contains(result.Lines[4].Differences, d => d.Kind == "Ilość ≠ desygnatory" && d.Severity == DifferenceSeverity.Warning);
        Assert.Equal(new[] { "U99" }, result.OnlyInPnp);
        Assert.Equal(2, result.Differences);
    }

    [Fact]
    public void MicroSignAndGreekMuAreEqualOnlyForComparison()
    {
        Assert.Equal(BomPnpComparer.Normalize("1µF"), BomPnpComparer.Normalize("1μf"));
        Assert.NotEqual(BomPnpComparer.Normalize("1uF"), BomPnpComparer.Normalize("1µF"));
    }

    [Theory]
    [InlineData("Designator\tX\tY\tRotation\tSide\r\nR1\t1.0\t2.0\t90\tTop\r\nR2\t3\t4\t0\tTop", 2)]
    [InlineData("\"Designator\",\"Footprint\",\"Mid X\"\n\"R1\",\"0402, mod\",\"10mm\"\n\"R2\",\"0402\",\"11mm\"", 2)]
    [InlineData("Designator   Footprint   X     Y\nR1           0402        1.0   2.0\nR2           0402        3.0   4.0", 2)]
    public void PnpTextReaderDetectsSeparatorsAndBuildsRows(string text, int expected)
    {
        var rows = PnpTextReader.Read(text);
        var table = PnpTextReader.ToTable(rows, rows[0].RowNumber);
        Assert.Equal("Designator", table.Headers[0]);
        var result = PnpImportBuilder.Build(table, [new(0, PnpField.Designator), new(1, PnpField.Footprint)]);
        Assert.False(result.HasErrors);
        Assert.Equal(expected, result.Rows.Count);
        Assert.Equal("R1", result.Rows[0].Designator);
    }

    [Fact]
    public void QuotedSeparatorStaysInsideCell()
    {
        var rows = PnpTextReader.Read("\"Designator\",\"Footprint\"\n\"R1\",\"0402, mod\"");
        Assert.Equal("0402, mod", rows[1].Values[1]);
    }

    [Fact]
    public void DuplicateDesignatorInPnpIsAnError()
    {
        var table = new BomSourceTable(["Des"], [new BomSourceRow(2, ["R1"]), new BomSourceRow(3, ["r1"])]);
        var result = PnpImportBuilder.Build(table, [new(0, PnpField.Designator)]);
        Assert.True(result.HasErrors);
        Assert.Contains(result.Issues, i => i.Severity == ImportSeverity.Error && i.Message.Contains("dwa razy"));
    }

    [Theory]
    [InlineData("RC0402FR-0710KL; ERJ-2RKF1002X", 2)]
    [InlineData("A|B|A", 2)]
    [InlineData("", 0)]
    [InlineData("1-2199230-6", 1)]
    public void MpnCellSplitsOnMainSeparators(string cell, int count) => Assert.Equal(count, MpnBases.Split(cell).Count);

    [Theory]
    [InlineData("118/2026", true)]
    [InlineData("7/2026", true)]
    [InlineData("118/26", false)]
    [InlineData("ZK118", false)]
    public void OrderNumberFormat(string value, bool valid) => Assert.Equal(valid, OrderNumbers.IsValid(value));
}
