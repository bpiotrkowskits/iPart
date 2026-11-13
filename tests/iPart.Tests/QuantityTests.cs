using System.Globalization;
using iPart.Core.Components;
using iPart.Core.Units;

namespace iPart.Tests;

public sealed class QuantityTests
{
    [Theory]
    [InlineData("100nF")] [InlineData("0.1uF")] [InlineData("100000pF")]
    [InlineData("0,1uF")] [InlineData("0.1µF")] [InlineData("0.1μF")]
    public void EquivalentCapacitances(string input) => Assert.Equal(100000m, ParameterInput.CapacitancePf(input));

    [Theory]
    [InlineData("1pF", "1")] [InlineData("1nF", "1000")] [InlineData("1uF", "1000000")]
    [InlineData("1mF", "1000000000")] [InlineData("1F", "1000000000000")]
    public void AllCapacitanceUnits(string input, string expected) =>
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), ParameterInput.CapacitancePf(input));

    [Theory]
    [InlineData("100R", "100")] [InlineData("4.7k", "4700")] [InlineData("4k7", "4700")]
    [InlineData("10M", "10000000")] [InlineData("0.3mR", "0.0003")] [InlineData("0R0", "0")]
    [InlineData("4,7k", "4700")] [InlineData("1M", "1000000")] [InlineData("1mR", "0.001")]
    public void ResistanceNotations(string input, string expected) =>
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), ParameterInput.ResistanceOhms(input));

    [Theory]
    [InlineData("1MF")] [InlineData("1nf")] [InlineData("1H")] [InlineData("1.234,5pF")]
    [InlineData("1,234.5pF")] [InlineData("1 000pF")] [InlineData("NaNpF")] [InlineData("")]
    [InlineData("1e3pF")] [InlineData("-1pF")] [InlineData("1,000,000pF")]
    public void InvalidInputNeverBecomesZero(string input) =>
        Assert.Throws<QuantityInputException>(() => ParameterInput.CapacitancePf(input));

    [Fact]
    public void SingleSeparatorIsDecimalNeverThousands()
    {
        Assert.Equal(1.234m, ParameterInput.CapacitancePf("1,234pF"));
        Assert.Equal(0m, ParameterInput.CapacitancePf(".0pF"));
        Assert.Equal(.5m, ParameterInput.CapacitancePf(",5pF"));
        Assert.Equal(.1m, QuantityParser.ParseNumber("+0.1000"));
        Assert.Throws<QuantityInputException>(() => ParameterInput.ResistanceOhms("1m"));
    }

    [Theory]
    [InlineData("ESR0025", "25", "ESR25")] [InlineData("ESR0070", "70", "ESR70")]
    [InlineData("ESR7500", "7500", "ESR7500")] [InlineData("ESR000,25", "0.25", "ESR0.25")]
    public void HistoricalEsrRemainsMilliohms(string input, string expected, string formatted)
    {
        var value = ParameterInput.EsrMilliOhms(input);
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), value);
        Assert.Equal(formatted, ParameterFormatter.HistoricalEsr(value));
    }

    [Theory]
    [InlineData("0.12345678901234567890123456789")]
    [InlineData("79228162514264337593543950336")]
    [InlineData("0.00000000000000000000000000001")]
    public void ParserRejectsSilentDecimalRounding(string input) =>
        Assert.Throws<QuantityInputException>(() => QuantityParser.ParseNumber(input));

    [Fact]
    public void ConversionAndStoragePrecisionAreValidated()
    {
        Assert.Throws<ComponentValidationException>(() => ParameterInput.CapacitancePf("0.0000000001pF"));
        Assert.Throws<ComponentValidationException>(() => ParameterInput.CapacitancePf("10000000F"));
        Assert.Throws<QuantityInputException>(() => ParameterInput.CapacitancePf("79228162514264337593543950335F"));
        Assert.Throws<QuantityInputException>(() => ParameterInput.ResistanceOhms("0.0000000000000000000000000001mR"));
    }
}
