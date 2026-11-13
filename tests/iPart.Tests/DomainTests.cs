using iPart.Core.Components;

namespace iPart.Tests;

public sealed class DomainTests
{
    [Theory]
    [InlineData("  001-A B /x  ", "001-A B /x")]
    [InlineData("\t\u00a0000 a\tb\n", "000 a\tb")]
    [InlineData("aBc", "aBc")]
    public void MpnTrimsOnlyEdges(string input, string expected)
    {
        var component = new Component { Mpn = input };
        Assert.Equal(expected, component.Mpn);
        Assert.True(MpnRules.Equal(input, expected));
    }

    [Fact]
    public void MpnEqualityIsOrdinalAndNullDimensionsAreNotWildcards()
    {
        Assert.False(MpnRules.Equal("abc", "ABC"));
        Assert.False(MpnRules.Equal("A B", "AB"));
        Assert.False(MpnRules.Equal("001", "1"));
        Assert.False(MpnRules.Equal("é", "e\u0301"));
    }

    [Theory]
    [InlineData(null, true)] [InlineData(0, true)] [InlineData(2, true)]
    [InlineData(-1, false)] [InlineData(1, false)] [InlineData(3, false)] [InlineData(int.MaxValue, false)]
    public void RcAndTt(int? value, bool valid)
    {
        CheckPads(new ComponentPads { RC = value }, valid);
        CheckPads(new ComponentPads { TT = value }, valid);
    }

    [Theory]
    [InlineData(null, true)] [InlineData(0, true)] [InlineData(1, false)] [InlineData(2, false)]
    [InlineData(3, true)] [InlineData(4, true)] [InlineData(int.MaxValue, true)] [InlineData(int.MinValue, false)]
    public void DlAndBga(int? value, bool valid)
    {
        CheckPads(new ComponentPads { DL = value }, valid);
        CheckPads(new ComponentPads { BGA = value }, valid);
    }

    [Theory]
    [InlineData(null, true)] [InlineData(0, true)] [InlineData(3, false)] [InlineData(4, true)]
    [InlineData(int.MaxValue, true)] [InlineData(-1, false)]
    public void Ql(int? value, bool valid) => CheckPads(new ComponentPads { QL = value }, valid);

    [Theory]
    [InlineData(null, true)] [InlineData(0, true)] [InlineData(1, true)]
    [InlineData(int.MaxValue, true)] [InlineData(-1, false)] [InlineData(int.MinValue, false)]
    public void OtherPadsHaveOnlyNonnegativeConstraint(int? value, bool valid)
    {
        foreach (var name in new[] { "TP", "THT", "PreTHT", "PressFIT", "MECH" })
        {
            var pads = new ComponentPads { BTC = false, NS = null };
            typeof(ComponentPads).GetProperty(name)!.SetValue(pads, value);
            CheckPads(pads, valid);
        }
    }

    [Theory]
    [InlineData(null, null, true)] [InlineData(0, null, true)] [InlineData(null, 0, true)]
    [InlineData(1, 2, false)] [InlineData(0, 0, false)]
    public void CapacitorTolerancesAreAlternative(int? percent, int? absolute, bool valid)
    {
        var component = new Component { Mpn = "test", Capacitor = new() { TolerancePercent = percent, AbsoluteTolerancePf = absolute } };
        if (valid) ComponentValidator.Validate(component);
        else Assert.Throws<ComponentValidationException>(() => ComponentValidator.Validate(component));
    }

    [Fact]
    public void DecimalLimitsAreCheckedWithoutRounding()
    {
        DecimalRules.Validate(9999999999999999999.999999999m, 28, 9, "pF");
        DecimalRules.Validate(0.000000001m, 28, 9, "pF");
        DecimalRules.Validate(1.0000000000m, 28, 9, "pF");
        Assert.Throws<ComponentValidationException>(() => DecimalRules.Validate(10000000000000000000m, 28, 9, "pF"));
        Assert.Throws<ComponentValidationException>(() => DecimalRules.Validate(.0000000001m, 28, 9, "pF"));
        Assert.Throws<ComponentValidationException>(() => DecimalRules.Validate(-1, 28, 9, "pF"));
        var component = new Component { Mpn = "test", Capacitor = new() { CapacitancePf = .0000000001m } };
        Assert.Throws<ComponentValidationException>(() => ComponentValidator.Validate(component));
    }

    [Fact]
    public void NullableBooleansAndZeroAreNotDefaults()
    {
        var component = new Component { Mpn = "test", Pads = new() };
        Assert.Null(component.Critical);
        Assert.Null(component.Rohs);
        Assert.Null(component.Pads.RC);
        Assert.Null(component.Pads.BTC);
        ComponentValidator.Validate(component);
    }

    private static void CheckPads(ComponentPads pads, bool valid)
    {
        if (valid) ComponentValidator.ValidatePads(pads);
        else Assert.Throws<ComponentValidationException>(() => ComponentValidator.ValidatePads(pads));
    }
}
