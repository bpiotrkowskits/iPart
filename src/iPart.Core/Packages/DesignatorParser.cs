using System.Text.RegularExpressions;
using iPart.Core.Import;

namespace iPart.Core.Packages;

public sealed record DesignatorNote(ImportSeverity Severity, string Message);
public sealed record DesignatorParseResult(IReadOnlyList<string> Designators, IReadOnlyList<DesignatorNote> Notes);

// Rozbija komórkę z desygnatorami na pojedyncze desygnatory.
// Obsługuje listy ("C1, C2A C3"), zakresy pełne ("R1-R5", "R01-R05", "C2A-C2D") i skrócone ("R1-5").
// Zasada: nie zgadujemy po cichu — skrócony zakres jest rozwijany z ostrzeżeniem,
// a zapis niebędący zakresem (np. "J1-1") pozostaje dosłowny.
public static partial class DesignatorParser
{
    public const int MaxRangeSize = 2000;

    [GeneratedRegex(@"^(?<p>[A-Za-z_]+)(?<n>\d+)(?<s>[A-Za-z]*)-(?<p2>[A-Za-z_]+)(?<n2>\d+)(?<s2>[A-Za-z]*)$")]
    private static partial Regex FullRange();

    [GeneratedRegex(@"^(?<p>[A-Za-z_]+)(?<n>\d+)-(?<n2>\d+)$")]
    private static partial Regex ShortRange();

    public static DesignatorParseResult Parse(string? text)
    {
        var result = new List<string>();
        var notes = new List<DesignatorNote>();
        if (string.IsNullOrWhiteSpace(text)) return new(result, notes);

        foreach (var token in text.Split([',', ';', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var full = FullRange().Match(token);
            if (full.Success && string.Equals(full.Groups["p"].Value, full.Groups["p2"].Value, StringComparison.Ordinal))
            {
                ExpandFull(token, full, result, notes);
                continue;
            }
            var shortRange = ShortRange().Match(token);
            if (shortRange.Success)
            {
                var start = int.Parse(shortRange.Groups["n"].Value);
                var end = int.Parse(shortRange.Groups["n2"].Value);
                if (end > start && end - start < MaxRangeSize)
                {
                    var prefix = shortRange.Groups["p"].Value;
                    for (var i = start; i <= end; i++) result.Add(prefix + i);
                    notes.Add(new(ImportSeverity.Warning, $"„{token}” rozwinięto jako zakres {prefix}{start}…{prefix}{end}. Sprawdź, czy to nie jest pojedynczy desygnator z myślnikiem."));
                    continue;
                }
            }
            result.Add(token);
        }

        var duplicates = result.GroupBy(x => x, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
        if (duplicates.Length > 0)
            notes.Add(new(ImportSeverity.Error, "Powtórzony desygnator w tej samej linii: " + string.Join(", ", duplicates)));
        return new(result, notes);
    }

    private static void ExpandFull(string token, Match m, List<string> result, List<DesignatorNote> notes)
    {
        var prefix = m.Groups["p"].Value;
        string n1 = m.Groups["n"].Value, n2 = m.Groups["n2"].Value;
        string s1 = m.Groups["s"].Value, s2 = m.Groups["s2"].Value;

        // C2A-C2D: ten sam numer, zakres liter na końcu.
        if (n1 == n2 && s1.Length == 1 && s2.Length == 1 && char.ToUpperInvariant(s1[0]) < char.ToUpperInvariant(s2[0])
            && char.IsUpper(s1[0]) == char.IsUpper(s2[0]))
        {
            for (var c = s1[0]; c <= s2[0]; c++) result.Add(prefix + n1 + c);
            return;
        }

        // R1-R5, R01-R05: zakres numerów bez liter na końcu.
        if (s1.Length == 0 && s2.Length == 0)
        {
            var start = int.Parse(n1);
            var end = int.Parse(n2);
            if (end > start && end - start < MaxRangeSize)
            {
                // Zera wiodące zachowujemy tylko, gdy oba końce mają tę samą szerokość (R01-R05 → R01…R05).
                var width = n1.Length == n2.Length && n1.StartsWith('0') ? n1.Length : 0;
                for (var i = start; i <= end; i++) result.Add(prefix + (width > 0 ? i.ToString().PadLeft(width, '0') : i.ToString()));
                return;
            }
        }

        result.Add(token);
        notes.Add(new(ImportSeverity.Warning, $"„{token}” wygląda jak zakres, ale nie da się go jednoznacznie rozwinąć. Pozostawiono bez zmian."));
    }
}
