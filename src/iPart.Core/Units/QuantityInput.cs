using System.Globalization;
using System.Text.RegularExpressions;
using iPart.Core.Components;

namespace iPart.Core.Units;

public readonly record struct ParsedQuantity(decimal Number, string Unit);
public sealed class QuantityInputException(string message) : Exception(message);

public static partial class QuantityParser
{
    [GeneratedRegex(@"^(?<number>[+]?(?:[0-9]+(?:[.,][0-9]+)?|[.,][0-9]+))\s*(?<unit>[a-zA-ZΩµμ]+)$", RegexOptions.CultureInvariant)]
    private static partial Regex QuantityPattern();
    [GeneratedRegex(@"^(?<whole>[0-9]+)(?<unit>R|k|M|G)(?<fraction>[0-9]+)$", RegexOptions.CultureInvariant)]
    private static partial Regex EmbeddedResistancePattern();
    [GeneratedRegex(@"^[+]?(?:[0-9]+(?:[.,][0-9]+)?|[.,][0-9]+)$", RegexOptions.CultureInvariant)]
    private static partial Regex NumberPattern();

    public static ParsedQuantity Parse(string input)
    {
        if (input is null) throw new QuantityInputException("Wartość nie może być pusta.");
        var match = QuantityPattern().Match(input.Trim());
        if (!match.Success) throw new QuantityInputException("Niepoprawny zapis wartości lub jednostki. Użyj jednego separatora dziesiętnego, bez separatorów tysięcy.");
        return new ParsedQuantity(ParseNumber(match.Groups["number"].Value), match.Groups["unit"].Value);
    }

    public static ParsedQuantity ParseResistance(string input)
    {
        if (input is null) throw new QuantityInputException("Rezystancja nie może być pusta.");
        var match = EmbeddedResistancePattern().Match(input.Trim());
        return match.Success
            ? new ParsedQuantity(ParseNumber(match.Groups["whole"].Value + "." + match.Groups["fraction"].Value), match.Groups["unit"].Value)
            : Parse(input);
    }

    // Historical ESR digits express milliohms, not ohms.
    public static ParsedQuantity ParseHistoricalEsr(string input)
    {
        if (input is null || !input.Trim().StartsWith("ESR", StringComparison.Ordinal))
            throw new QuantityInputException("Historyczne oznaczenie ESR musi zaczynać się od ESR.");
        return new ParsedQuantity(ParseNumber(input.Trim()[3..]), "mΩ");
    }

    public static decimal ParseNumber(string input)
    {
        if (!NumberPattern().IsMatch(input)) throw new QuantityInputException("Niepoprawna liczba: dozwolony jest jeden separator dziesiętny (kropka lub przecinek).");
        var normalized = input.Replace(',', '.');
        // decimal.TryParse may silently round overly precise inputs. Check the exact decimal digits first.
        var unsigned = normalized.TrimStart('+');
        var pieces = unsigned.Split('.');
        var fraction = pieces.Length == 2 ? pieces[1].TrimEnd('0') : "";
        var digits = (pieces[0] + fraction).TrimStart('0');
        if (fraction.Length > 28 || digits.Length > 29 ||
            !decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var value))
            throw new QuantityInputException("Liczba przekracza zakres lub dokładność typu decimal.");
        // Verify that parsing did not round a 29-digit fractional number.
        var coefficientDigits = pieces[0] + fraction;
        if (!System.Numerics.BigInteger.TryParse(coefficientDigits.Length == 0 ? "0" : coefficientDigits, out var coefficient))
            throw new QuantityInputException("Niepoprawna liczba.");
        var bits = decimal.GetBits(value);
        var parsedCoefficient = (System.Numerics.BigInteger)(uint)bits[0]
            + ((System.Numerics.BigInteger)(uint)bits[1] << 32)
            + ((System.Numerics.BigInteger)(uint)bits[2] << 64);
        var parsedScale = (bits[3] >> 16) & 0xff;
        if (coefficient * System.Numerics.BigInteger.Pow(10, parsedScale) !=
            parsedCoefficient * System.Numerics.BigInteger.Pow(10, fraction.Length))
            throw new QuantityInputException("Liczba wymagałaby utraty dokładności.");
        return value;
    }
}

public static class QuantityConverter
{
    public static decimal CapacitanceToPf(ParsedQuantity value) => Multiply(value, value.Unit switch
    {
        "pF" => 1m, "nF" => 1000m, "uF" or "µF" or "μF" => 1000000m,
        "mF" => 1000000000m, "F" => 1000000000000m,
        _ => throw Unknown(value.Unit)
    });
    public static decimal ResistanceToOhms(ParsedQuantity value) => Multiply(value, value.Unit switch
    {
        "mR" => 0.001m, "R" or "Ω" => 1m, "k" => 1000m, "M" => 1000000m, "G" => 1000000000m,
        _ => throw Unknown(value.Unit)
    });
    public static decimal EsrToMilliOhms(ParsedQuantity value) => value.Unit == "mΩ" ? value.Number : throw Unknown(value.Unit);

    private static decimal Multiply(ParsedQuantity value, decimal factor)
    {
        try
        {
            var result = checked(value.Number * factor);
            if (result / factor != value.Number) throw new QuantityInputException("Przeliczenie jednostki spowodowałoby utratę dokładności.");
            return result;
        }
        catch (OverflowException) { throw new QuantityInputException("Wartość po przeliczeniu jednostki przekracza zakres decimal."); }
    }
    private static QuantityInputException Unknown(string unit) => new($"Nieznana jednostka „{unit}”. Wielkość liter w prefiksach ma znaczenie.");
}

// Entry points for a future editor/importer: parser -> conversion -> storage validation.
public static class ParameterInput
{
    public static decimal CapacitancePf(string input) => Validate(QuantityConverter.CapacitanceToPf(QuantityParser.Parse(input)), 28, 9, "CapacitancePf");
    public static decimal ResistanceOhms(string input) => Validate(QuantityConverter.ResistanceToOhms(QuantityParser.ParseResistance(input)), 28, 12, "ResistanceOhms");
    public static decimal EsrMilliOhms(string input) => Validate(QuantityConverter.EsrToMilliOhms(QuantityParser.ParseHistoricalEsr(input)), 28, 9, "EsrMilliOhms");
    private static decimal Validate(decimal value, int precision, int scale, string field)
    {
        DecimalRules.Validate(value, precision, scale, field);
        return value;
    }
}
