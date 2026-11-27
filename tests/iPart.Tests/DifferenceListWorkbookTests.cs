using iPart.Core.Packages;
using iPart.Infrastructure.Packages;

namespace iPart.Tests;

public sealed class DifferenceListWorkbookTests
{
    [Fact]
    public void WrittenFileHasTemplateHeadersAndAnswersAreReadBackByBomDesignators()
    {
        var workbook = new DifferenceListWorkbook();
        var content = workbook.Write("Wykaz Różnic", [
            new DifferenceListRow(1, "R1–R3", "R1–R2", "10k", "10k", "0402", "0603", null, null, "Który footprint?", null),
            new DifferenceListRow(2, "U1", "", "MCU", null, null, null, null, null, "Czy montowany?", "Tak, ręcznie")]);

        var sheet = BomWorkbookReader.Read(content).Single();
        Assert.Equal("Designator\nBOM", sheet.Rows[0].Values[0]);
        Assert.Equal("ODPOWIEDŹ", sheet.Rows[0].Values[10]);
        Assert.Equal("Który footprint?", sheet.Rows[1].Values[8]);

        // Odpowiedź Klienta wraca po Designator BOM; wiersze bez odpowiedzi pomijane.
        Assert.Equal(("U1", "Tak, ręcznie"), Assert.Single(workbook.ReadAnswers(content)));
    }

    [Fact]
    public void LineFieldsDifferOnlyWhenPnpHasOtherValue()
    {
        var line = new BomLine { LineNumber = 1, Value = "10k", Footprint = "0402", ExpandedDesignators = [new() { Designator = "R1" }, new() { Designator = "R2" }] };
        var pnp = new List<PnpRow> { new() { Designator = "R1", Value = "10K", Footprint = "0603" }, new() { Designator = "R2", Value = "10k", Footprint = "0805" } };
        var fields = LineFields.For(line, pnp);

        Assert.False(fields.Single(f => f.Field == ComparedFields.Value).Differs);
        var footprint = fields.Single(f => f.Field == ComparedFields.Footprint);
        Assert.True(footprint.Differs);
        Assert.Null(LineFields.Value(footprint, new(ComparedFields.Footprint, ValueSource.Pnp, null))); // dwie różne wartości w P&P
        Assert.Equal("0402", LineFields.Value(footprint, new(ComparedFields.Footprint, ValueSource.Bom, null)));
        Assert.Equal("0603", LineFields.Value(footprint, new(ComparedFields.Footprint, ValueSource.Own, " 0603 ")));
    }
}
