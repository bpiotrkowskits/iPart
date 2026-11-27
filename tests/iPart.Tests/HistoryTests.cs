using iPart.Core.Components;

namespace iPart.Tests;

public sealed class HistoryTests
{
    [Fact]
    public void TracksParametersPadsAndRelationshipsAndDistinguishesNullZeroFalse()
    {
        var old = new Component { Mpn = "001", Pads = new(), Resistor = new() };
        var current = new Component { Id = old.Id, Mpn = "001", Pads = new() { TP = 0, BTC = false },
            Resistor = new() { ResistanceOhms = 0 }, ManufacturerId = 1, Qualifications = [new() { QualificationId = 1 }],
            Links = [new() { Url = "https://example.org", Kind = ComponentLinkKind.Documentation }] };
        var changes = ComponentChanges.Between(old, current);
        Assert.Contains(changes, x => x.Path == "Pads.TP" && x.PreviousValue is null && x.NewValue == "0");
        Assert.Contains(changes, x => x.Path == "Pads.BTC" && x.PreviousValue is null && x.NewValue == "false");
        Assert.Contains(changes, x => x.Path == "Resistor.ResistanceOhms" && x.NewValue == "0");
        Assert.Contains(changes, x => x.Path == "ManufacturerId");
        Assert.Contains(changes, x => x.Path == "Qualifications.1");
        Assert.Contains(changes, x => x.Path.EndsWith(".Url"));
        Assert.DoesNotContain(changes, x => x.Path == "Mpn" || x.Path.Contains("Version"));
    }

    [Fact]
    public void EqualDecimalsAndVersionAreNotBusinessChanges()
    {
        var old = new Component { Mpn = "X", Resistor = new() { ResistanceOhms = 1.00m } };
        var current = new Component { Id = old.Id, Mpn = "X", Version = Guid.NewGuid(), Resistor = new() { ResistanceOhms = 1m } };
        Assert.Empty(ComponentChanges.Between(old, current));
    }

    [Fact]
    public void RemovalOfOptionalSetAndLinkIsAudited()
    {
        var old = new Component { Mpn = "X", Pads = new(), Links = [new() { Url = "https://example.org" }] };
        var current = new Component { Mpn = "X", Id = old.Id };
        var changes = ComponentChanges.Between(old, current);
        Assert.Contains(changes, x => x.Path == "Pads.Exists" && x.PreviousValue == "true" && x.NewValue is null);
        Assert.Contains(changes, x => x.Path.EndsWith(".Url") && x.NewValue is null);
    }
}
