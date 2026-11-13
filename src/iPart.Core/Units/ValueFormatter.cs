using System.Globalization;
using iPart.Core.Components;

namespace iPart.Core.Units;

public interface IComponentValueFormatter
{
    string? FormatResistor(ResistorParameters? parameters, IEnumerable<string>? aecQualifications = null);
}

public sealed class ComponentValueFormatter : IComponentValueFormatter
{
    public string? FormatResistor(ResistorParameters? parameters, IEnumerable<string>? aecQualifications = null)
    {
        var parts = new List<string?>
        {
            ParameterFormatter.Resistance(parameters?.ResistanceOhms),
            ParameterFormatter.Fixed(parameters?.TolerancePercent, "%"),
            ParameterFormatter.Power(parameters?.PowerWatts)
        };
        parts.AddRange((aecQualifications ?? []).Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
        parts.Add(ParameterFormatter.Fixed(parameters?.TemperatureCoefficientPpmPerC, "ppm"));
        var result = string.Join('_', parts.Where(x => !string.IsNullOrEmpty(x)));
        return result.Length == 0 ? null : result;
    }
}

public static class ParameterFormatter
{
    private readonly record struct Unit(decimal Factor, string Symbol);
    private static readonly Unit[] ResistanceUnits = [new(.001m, "mR"), new(1, "R"), new(1000, "k"), new(1000000, "M"), new(1000000000, "G")];
    private static readonly Unit[] CapacitanceUnits = [new(1, "pF"), new(1000, "nF"), new(1000000, "uF"), new(1000000000, "mF"), new(1000000000000, "F")];
    private static readonly Unit[] PowerUnits = [new(.001m, "mW"), new(1, "W"), new(1000, "kW")];

    public static string? Resistance(decimal? ohms) => Scaled(ohms, ResistanceUnits);
    public static string? Capacitance(decimal? pf) => Scaled(pf, CapacitanceUnits);
    public static string? Power(decimal? watts) => Scaled(watts, PowerUnits);
    public static string? HistoricalEsr(decimal? milliOhms) => milliOhms is null ? null : "ESR" + Number(milliOhms.Value);
    public static string? Fixed(decimal? value, string suffix) => value is null ? null : Number(value.Value) + suffix;

    private static string? Scaled(decimal? value, Unit[] units)
    {
        if (value is null) return null;
        // Use the base unit for an actual zero; null remains absent.
        if (value == 0) return "0" + units.First(x => x.Factor == 1).Symbol;
        var magnitude = Math.Abs(value.Value);
        var index = 0;
        while (index + 1 < units.Length && magnitude >= units[index + 1].Factor) index++;
        var scaled = value.Value / units[index].Factor;
        if (Math.Abs(decimal.Round(scaled, 2, MidpointRounding.AwayFromZero)) >= 1000 && index + 1 < units.Length)
            scaled = value.Value / units[++index].Factor;
        return Number(scaled) + units[index].Symbol;
    }

    private static string Number(decimal value)
    {
        var rounded = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
        return (rounded == 0 && value != 0 ? value : rounded)
            .ToString("0.############################", CultureInfo.InvariantCulture);
    }
}
