namespace iPart.Core.Components;

/// <summary>
/// Grupy sposobów montażu używane przez reguły widoków (kolory, polityka wydań).
/// Porównanie bez wielkości liter, spacji i myślników, bo w plikach Excel i w słowniku bazy
/// ta sama wartość bywa zapisana różnie („PressFit” / „Press-Fit”, „MECH” / „Mech”).
/// </summary>
public static class MountingCategories
{
    private static readonly HashSet<string> NonSmt = new(StringComparer.Ordinal) { "THT", "PRESSFIT", "MECH", "PLUGS" };

    /// <summary>
    /// Montaż THT, Press-Fit, MECH albo Plugs (wtyki, domyślnie wydawane na THT) — oznaczany kolorem
    /// w liniach BOM (decyzja 8.1) i wydawany na styk w kompletacji (decyzja 8.3).
    /// </summary>
    public static bool IsNonSmt(string? mounting) => mounting is not null && NonSmt.Contains(Normalize(mounting));

    private static string Normalize(string value) =>
        new string(value.Where(c => c != '-' && !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
}
