using iPart.Core.Components;
using iPart.Core.Import;
using iPart.Core.Packages;

namespace iPart.Tests;

// Comparator: wybór porównywanych pól, pusty Footprint w BOM (przyjęcie z P&P albo rozdzielenie linii), iPart roboczy.
public sealed class ComparatorTests
{
    private static BomLine Line(string designators, string? footprint = null, string? value = null, string? description = null)
    {
        var line = new BomLine { LineNumber = 1, Designators = designators, Footprint = footprint, Value = value, Description = description };
        line.ExpandedDesignators = DesignatorParser.Parse(designators).Designators.Select(d => new BomLineDesignator { BomLineId = line.Id, Designator = d }).ToList();
        return line;
    }

    private static IEnumerable<PnpRow> Rows(string prefix, int from, int to, string? footprint, string? value = null, string? description = null) =>
        Enumerable.Range(from, to - from + 1).Select(i => new PnpRow { Designator = $"{prefix}{i}", Footprint = footprint, Value = value, Description = description });

    [Fact]
    public void PnpTextKeepsQuotedValuesWithSpacesAndLineBreaksInOneCell()
    {
        var text = string.Join("\r\n",
            "Designator Comment Layer Footprint Center-X(mil) Center-Y(mil) Rotation Description",
            "TP_1V2    TESTPOINT        TopLayer    TESTPOINT       8948.819  10933.071  270  \"PCB testpoint\"",
            "R150      \"RES0402 1%\"     BottomLayer RESC1005X04N    6938.987  12133.858  270  \"0402 Resistor\"",
            "C147      LGA2W391MELA50   TopLayer    LGA2W391MELA50  2350.394  10196.850  0    \"Aluminum Electrolytic Capacitor, Polarized, 20% -Tol,",
            "390uF, Through Hole Mount\"",
            "CAL_UI_L3 5104TR           TopLayer    5104_Keystone   10122.047 4385.827   270  \"0.02 in 0.5 mm Thick Copper\"");
        var rows = PnpTextReader.Read(text);

        Assert.Equal(5, rows.Count);
        Assert.Equal(["R150", "RES0402 1%", "BottomLayer", "RESC1005X04N", "6938.987", "12133.858", "270", "0402 Resistor"], rows[2].Values);
        Assert.Equal("Aluminum Electrolytic Capacitor, Polarized, 20% -Tol, 390uF, Through Hole Mount", rows[3].Values[7]);
        Assert.Equal(6, rows[4].RowNumber);
    }

    [Fact]
    public void PartNumberIsComparedAsWholeTextOnlyWhenChosenAndGroupShowsBomAndPnpSeparately()
    {
        var line = Line("L11-L13", description: "Inductor: wire; THT; 22uH");
        line.PartNumber = "DTMSS-20; ALT-1";
        var pnp = Rows("L", 11, 13, null, description: "Inductor: toroidal").ToList();
        foreach (var p in pnp) p.PartNumber = "DTMSS-20";

        var withoutPn = BomPnpComparer.Compare([line], pnp).Lines[0];
        Assert.DoesNotContain(withoutPn.Groups.SelectMany(g => g.Details), d => d.Field == ComparedFields.PartNumber);
        var group = Assert.Single(BomPnpComparer.Compare([line], pnp, new ComparatorOptions(true, true, true, true)).Lines[0].Groups);
        Assert.Equal([ComparedFields.Description, ComparedFields.PartNumber], group.Details.Select(d => d.Field).Order());
        var description = group.Details.Single(d => d.Field == ComparedFields.Description);
        Assert.Equal(("BOM: Inductor: wire; THT; 22uH", "P&P: Inductor: toroidal"), (description.BomText, description.PnpText));
        Assert.Equal("L11–L13", group.Designators);
        Assert.Equal("Value,Footprint,Description,PartNumber", new ComparatorOptions(true, true, true, true).Store());
    }

    [Fact]
    public void UncheckedFieldIsNotADifference()
    {
        var line = Line("R1-R2", "0402", "10k", "RES 10k");
        var pnp = Rows("R", 1, 2, "0402", "10k", "Resistor 10k").ToList();

        Assert.NotEmpty(BomPnpComparer.Compare([line], pnp).Lines[0].Differences);
        Assert.Empty(BomPnpComparer.Compare([line], pnp, new ComparatorOptions(true, true, false)).Lines[0].Differences);
        Assert.False(LineFields.For(line, pnp, new ComparatorOptions(true, true, false)).Single(f => f.Field == ComparedFields.Description).Differs);
    }

    [Fact]
    public void EmptyBomFootprintIsTakenFromPnpWhenAllDesignatorsAgree()
    {
        var line = Line("R1-R10", value: "100R");
        var pnp = Rows("R", 1, 10, "0402", "100R").ToList();

        var footprint = LineFields.For(line, pnp).Single(f => f.Field == ComparedFields.Footprint);
        Assert.True(footprint.TakenFromPnp);
        Assert.False(footprint.Differs);
        Assert.Empty(BomPnpComparer.Compare([line], pnp).Lines[0].Differences);
    }

    [Fact]
    public void EmptyBomFootprintWithDifferentPnpFootprintsRequiresSplit()
    {
        // BOM: R1–R10 100R/1%, bez Footprint. P&P: R1–R5 0402, R6–R10 0603 → dwie linie.
        var line = Line("R1-R10", value: "100R");
        var pnp = Rows("R", 1, 5, "0402", "100R").Concat(Rows("R", 6, 10, "0603", "100R")).ToList();

        var comparison = BomPnpComparer.Compare([line], pnp).Lines[0];
        Assert.Contains(comparison.Differences, d => d.Kind == BomPnpComparer.SplitKind);
        Assert.Equal(2, comparison.Groups.Count);
        var groups = LineFields.SplitGroups(line, pnp);
        Assert.Equal(["R1–R5", "R6–R10"], groups.Select(DesignatorRanges.Compress).Order());
    }

    [Fact]
    public void OptionsAreStoredAndParsed()
    {
        var options = new ComparatorOptions(true, false, true);
        Assert.Equal("Value,Description", options.Store());
        Assert.Equal(options, ComparatorOptions.Parse(options.Store()));
        Assert.Equal(ComparatorOptions.AllFields, ComparatorOptions.Parse(null));
    }

    [Theory]
    [InlineData(10000.0, null, null, "10k")]
    [InlineData(4.7, null, null, "4.7R")]
    [InlineData(null, 100000.0, null, "100nF")]
    [InlineData(null, 2200000.0, null, "2.2µF")]
    [InlineData(null, null, 4.7, "4.7µH")]
    public void ComponentValueIsDescribedFromParameters(double? ohms, double? pf, double? uh, string expected)
    {
        var component = new Component
        {
            Mpn = "X",
            Resistor = ohms is null ? null : new ResistorParameters { ResistanceOhms = (decimal)ohms },
            Capacitor = pf is null ? null : new CapacitorParameters { CapacitancePf = (decimal)pf },
            Inductor = uh is null ? null : new InductorParameters { InductanceMicroHenries = (decimal)uh },
        };
        Assert.Equal(expected, ComponentValues.Describe(component));
    }

    [Fact]
    public void CoreColumnsComeFromDatabaseOrCustomerData()
    {
        var line = Line("C1", "0402", "100n");
        line.PartNumber = "GRM155";
        var mpns = new List<LineMpn> { new() { BomLineId = line.Id, Mpn = "GRM155", Ordinal = 1 } };

        var fromCustomer = LineCore.For(line, mpns, new Dictionary<string, ComponentCore>());
        Assert.Equal(("0402", LineCore.FromCustomer), (fromCustomer.Case.Value, fromCustomer.Case.Source));
        Assert.Equal(("0402", LineCore.FromCase), (fromCustomer.Footprint.Value, fromCustomer.Footprint.Source));
        Assert.Equal("100n", fromCustomer.Value.Value);

        var fromDatabase = LineCore.For(line, mpns, new Dictionary<string, ComponentCore> { ["GRM155"] = new("GRM155", "0402", "C0402", "100nF") });
        Assert.Equal(("C0402", LineCore.FromDatabase), (fromDatabase.Footprint.Value, fromDatabase.Footprint.Source));
        Assert.Equal(("100nF", LineCore.FromDatabase), (fromDatabase.Value.Value, fromDatabase.Value.Source));
        Assert.Equal("GRM155", fromDatabase.PartNumber);
    }
}
