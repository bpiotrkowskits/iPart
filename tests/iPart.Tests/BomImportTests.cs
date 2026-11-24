using iPart.Core.Import;
using iPart.Core.Packages;

namespace iPart.Tests;

public sealed class DesignatorParserTests
{
    [Theory]
    [InlineData("C1, C2A, C3", new[] { "C1", "C2A", "C3" })]
    [InlineData("R1-R4", new[] { "R1", "R2", "R3", "R4" })]
    [InlineData("R01-R03", new[] { "R01", "R02", "R03" })]
    [InlineData("C2A-C2D", new[] { "C2A", "C2B", "C2C", "C2D" })]
    [InlineData("R9-R11 C1;C2", new[] { "R9", "R10", "R11", "C1", "C2" })]
    [InlineData("J1-1", new[] { "J1-1" })]
    [InlineData("", new string[0])]
    public void ExpandsListsAndRanges(string text, string[] expected) =>
        Assert.Equal(expected, DesignatorParser.Parse(text).Designators);

    [Fact]
    public void ShortRangeIsExpandedWithWarning()
    {
        var result = DesignatorParser.Parse("R1-3");
        Assert.Equal(new[] { "R1", "R2", "R3" }, result.Designators);
        Assert.Contains(result.Notes, n => n.Severity == ImportSeverity.Warning);
    }

    [Fact]
    public void AmbiguousRangeStaysLiteralWithWarning()
    {
        var result = DesignatorParser.Parse("C1A-C3B");
        Assert.Equal(new[] { "C1A-C3B" }, result.Designators);
        Assert.Contains(result.Notes, n => n.Severity == ImportSeverity.Warning);
    }

    [Fact]
    public void DuplicateInLineIsError() =>
        Assert.Contains(DesignatorParser.Parse("R1, R1-R2").Notes, n => n.Severity == ImportSeverity.Error);
}

public sealed class BomImportBuilderTests
{
    private static BomSourceTable Table(string[] headers, params string?[][] rows) =>
        new(headers, rows.Select((r, i) => new BomSourceRow(i + 2, r)).ToList());

    [Fact]
    public void MapsColumnsKeepsOrderAndSendsUnmappedToExtras()
    {
        var table = Table(["Ref", "Qty", "MPN", "Comment", "Supplier"],
            ["R1-R2", "2", "RC0402", "10k", "X"],
            [null, null, null, null, null],
            ["C1", "1", null, "100nF", null]);
        var result = BomImportBuilder.Build(table, [
            new(0, BomField.Designator), new(1, BomField.Quantity), new(2, BomField.PartNumber), new(3, BomField.Value)]);

        Assert.False(result.HasErrors);
        Assert.Equal(new[] { 1, 2 }, result.Lines.Select(l => l.LineNumber));
        Assert.Equal(2, result.Lines[0].ExpandedDesignators.Count);
        Assert.Equal("RC0402", result.Lines[0].PartNumber);
        var extra = Assert.Single(result.Lines[0].ExtraValues);
        Assert.Equal(("Supplier", "X", 1), (extra.Header, extra.Value, extra.Ordinal));
        Assert.Empty(result.Lines[1].ExtraValues);
    }

    [Fact]
    public void TwoColumnsIntoOneFieldUsePriority()
    {
        // Aktywne: Part Number; pasywne: wartość — dwie kolumny Klienta do jednego pola Part TS.
        var table = Table(["Des", "PN", "Val"], ["U1", "STM32", "MCU"], ["R1", null, "10k"]);
        var result = BomImportBuilder.Build(table, [
            new(0, BomField.Designator), new(1, BomField.PartTs, 1), new(2, BomField.PartTs, 2)]);
        Assert.Equal(new[] { "STM32", "10k" }, result.Lines.Select(l => l.PartTs));
    }

    [Fact]
    public void SamePriorityForOneFieldIsError()
    {
        var table = Table(["Des", "A", "B"], ["U1", "x", "y"]);
        var result = BomImportBuilder.Build(table, [new(0, BomField.Designator), new(1, BomField.PartTs, 1), new(2, BomField.PartTs, 1)]);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void DesignatorInTwoLinesIsErrorAndQuantityMismatchIsWarning()
    {
        var table = Table(["Des", "Qty"], ["R1, R2", "3"], ["R2", "1"]);
        var result = BomImportBuilder.Build(table, [new(0, BomField.Designator), new(1, BomField.Quantity)]);
        Assert.True(result.HasErrors);
        Assert.Contains(result.Issues, i => i.Severity == ImportSeverity.Warning && i.LineNumber == 1);
    }

    [Fact]
    public void MissingDesignatorMappingAndBadQuantityAreErrors()
    {
        var table = Table(["Qty"], ["abc"]);
        var result = BomImportBuilder.Build(table, [new(0, BomField.Quantity)]);
        Assert.Contains(result.Issues, i => i.Severity == ImportSeverity.Error && i.Message.Contains("Designator"));
        Assert.Contains(result.Issues, i => i.Severity == ImportSeverity.Error && i.Message.Contains("abc"));
    }
}
