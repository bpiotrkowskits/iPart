using System.Globalization;
using iPart.Core.Components;
using iPart.Core.Units;

namespace iPart.Tests;

public sealed class FormattingTests
{
    [Theory]
    [InlineData("0.0003", "0.3mR")] [InlineData("1000", "1k")]
    [InlineData("4700", "4.7k")] [InlineData("10000000", "10M")]
    [InlineData("999.994", "999.99R")] [InlineData("999.995", "1k")]
    [InlineData("999999.995", "1M")]
    [InlineData("0.000000000001", "0.000000001mR")]
    [InlineData("0", "0R")] [InlineData("1000000000000", "1000G")]
    public void ResistanceScaling(string number, string expected) =>
        Assert.Equal(expected, ParameterFormatter.Resistance(decimal.Parse(number, CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData("1000", "1nF")] [InlineData("100000", "100nF")]
    [InlineData("1000000", "1uF")] [InlineData("999.995", "1nF")]
    [InlineData("0.000000001", "0.000000001pF")]
    public void CapacitanceScaling(string number, string expected) =>
        Assert.Equal(expected, ParameterFormatter.Capacitance(decimal.Parse(number, CultureInfo.InvariantCulture)));

    [Fact]
    public void PowerAndFixedFields()
    {
        Assert.Equal("250mW", ParameterFormatter.Power(.25m));
        Assert.Equal("1W", ParameterFormatter.Power(.999995m));
        Assert.Equal("1kW", ParameterFormatter.Power(999.995m));
        Assert.Equal("1000%", ParameterFormatter.Fixed(1000, "%"));
        Assert.Equal("ESR7500", ParameterFormatter.HistoricalEsr(7500));
        Assert.Equal("1.01%", ParameterFormatter.Fixed(1.005m, "%"));
        Assert.Equal("-1.01ppm", ParameterFormatter.Fixed(-1.005m, "ppm"));
        Assert.Null(ParameterFormatter.Resistance(null));
        Assert.Null(ParameterFormatter.Capacitance(null));
    }

    [Theory]
    [InlineData("pl-PL")] [InlineData("en-US")] [InlineData("tr-TR")] [InlineData("de-DE")]
    public void FullValueDoesNotDependOnCulture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var parameters = new ResistorParameters { ResistanceOhms = 10000000, TolerancePercent = 1, PowerWatts = .25m, TemperatureCoefficientPpmPerC = 100 };
            Assert.Equal("10M_1%_250mW_AEC-Q200_100ppm", new ComponentValueFormatter().FormatResistor(parameters, ["AEC-Q200"]));
            parameters.ResistanceOhms = 4700;
            Assert.StartsWith("4.7k_", new ComponentValueFormatter().FormatResistor(parameters));
            Assert.Equal(4700, parameters.ResistanceOhms);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void AbsentPartsProduceNoEmptySeparators()
    {
        var formatter = new ComponentValueFormatter();
        Assert.Equal("10M_100ppm", formatter.FormatResistor(new() { ResistanceOhms = 10000000, TemperatureCoefficientPpmPerC = 100 }));
        Assert.Null(formatter.FormatResistor(new()));
        Assert.Null(formatter.FormatResistor(null));
        Assert.Equal("AEC-Q200", formatter.FormatResistor(null, ["AEC-Q200", "AEC-Q200", ""]));
        Assert.Equal("0R_0%_0W_0ppm", formatter.FormatResistor(new() { ResistanceOhms = 0, TolerancePercent = 0, PowerWatts = 0, TemperatureCoefficientPpmPerC = 0 }));
    }
}
