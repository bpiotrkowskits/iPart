using System.Globalization;

namespace iPart.Core.Components;

/// <summary>
/// Value komponentu z parametrów w bazie, w zapisie jak w BOM: rezystor „10k”, kondensator „100nF”, cewka „4.7µH”, kwarc „16MHz”.
/// Brak parametrów → null (wtedy iPart bierze Value z danych Klienta).
/// </summary>
public static class ComponentValues
{
    public static string? Describe(Component component)
    {
        if (component.Resistor?.ResistanceOhms is { } ohms) return Scale(ohms, [(1e6m, "M"), (1e3m, "k"), (1m, "R")], "", "R");
        if (component.Capacitor?.CapacitancePf is { } pf) return Scale(pf, [(1e6m, "µ"), (1e3m, "n"), (1m, "p")], "F", "p");
        if (component.Inductor?.InductanceMicroHenries is { } uh) return Scale(uh * 1000m, [(1e6m, "m"), (1e3m, "µ"), (1m, "n")], "H", "n");
        if (component.Crystal?.FrequencyHz is { } hz) return Scale(hz, [(1e6m, "M"), (1e3m, "k"), (1m, "")], "Hz", "");
        return null;
    }

    // Największa jednostka, dla której liczba ≥ 1; bez zbędnych zer („4.7”, „100”). Rezystory poniżej 1 Ω: „0.1R”.
    private static string Scale(decimal value, (decimal Factor, string Prefix)[] units, string unit, string smallest)
    {
        foreach (var (factor, prefix) in units)
            if (Math.Abs(value) >= factor) return Format(value / factor) + prefix + unit;
        return Format(value) + smallest + unit;
    }

    private static string Format(decimal value) => Math.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);
}
