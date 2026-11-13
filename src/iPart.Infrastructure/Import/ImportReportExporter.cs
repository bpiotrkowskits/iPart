using System.Globalization;
using System.Text;
using System.Text.Json;
using iPart.Core.Import;

namespace iPart.Infrastructure.Import;

public static class ImportReportExporter
{
    public static async Task ExportAsync(ImportReport report, string path, CancellationToken cancellationToken = default)
    {
        var destination = Path.GetFullPath(path);
        if (string.Equals(destination, Path.GetFullPath(report.FilePath), StringComparison.OrdinalIgnoreCase))
            throw new IOException("Raport nie może zastąpić skoroszytu źródłowego.");
        if (!string.Equals(Path.GetExtension(destination), ".json", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(Path.GetExtension(destination), ".csv", StringComparison.OrdinalIgnoreCase))
            throw new IOException("Wybierz rozszerzenie raportu .json albo .csv.");
        await using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
        if (Path.GetExtension(destination).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            await JsonSerializer.SerializeAsync(output, report, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
            return;
        }
        await using var writer = new StreamWriter(output, new UTF8Encoding(true));
        async Task Row(params string?[] cells) => await writer.WriteLineAsync(string.Join(';', cells.Select(Csv)).AsMemory(), cancellationToken);
        await Row("Rodzaj", "Arkusz", "Wiersz", "Komórka", "Nagłówek/pole", "Oryginał/wartość", "Cache formuły", "Format", "Mapowanie/ważność", "Szczegóły");
        await Row("Podsumowanie", null, null, null, "partsData", report.PartsDataRecords.ToString(CultureInfo.InvariantCulture), null, null, null,
            $"Gotowe bez błędów={report.ReadyRecords}; z ostrzeżeniami={report.WarningRecords}; z błędami={report.BlockedRecords}; grup duplikatów={report.DuplicateGroups}; kolizji z bazą={report.DatabaseCollisions}; niedopasowanych pomocniczych={report.UnmatchedAuxiliaryRows}; problemów={report.ProblemCount}; dotkniętych rekordów={report.AffectedRecords}; SHA256={report.InputSha256}");
        await Row("Mapowanie", null, null, null, "Decyzje użytkownika", $"ThP → TP={report.Options.ConfirmThPToTp}; NonS → NS={report.Options.ConfirmNonSToNs}");
        foreach (var sheet in report.Sheets)
            await Row("Arkusz", sheet.Name, null, null, null, sheet.DataRows.ToString(CultureInfo.InvariantCulture), null, null, sheet.Purpose, $"Pominięte wiersze wyłącznie formuł: {sheet.IgnoredFormulaRows}");
        foreach (var record in report.Records)
        {
            await Row("Rekord", "partsData", record.SourceRow.ToString(CultureInfo.InvariantCulture), null, "MPN", record.Mpn, null, null, record.Status, record.GeneratedValue);
            // JSON preserves the complete typed proposal and per-field provenance even in the CSV report.
            await Row("Propozycja", "partsData", record.SourceRow.ToString(CultureInfo.InvariantCulture), null, "Component", JsonSerializer.Serialize(record.Component));
            foreach (var (field, sources) in record.FieldSources)
                foreach (var source in sources) await Row("Pochodzenie", source.Sheet, source.Row.ToString(CultureInfo.InvariantCulture), source.Address, field, source.OriginalValue, source.Value, source.NumberFormat, source.Mapping, $"partsData:{record.SourceRow}");
        }
        foreach (var source in report.AllSourceCells)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Row("Źródło", source.Sheet, source.Row.ToString(CultureInfo.InvariantCulture), source.Address, source.Header, source.OriginalValue, source.Value, source.NumberFormat, source.Mapping);
        }
        foreach (var issue in report.Issues)
        {
            await Row("Problem", null, issue.PartRow?.ToString(CultureInfo.InvariantCulture), issue.Locations, issue.Code, null, null, null, issue.SeverityLabel, issue.Message);
            foreach (var source in issue.Sources)
                await Row("Źródło problemu", source.Sheet, source.Row.ToString(CultureInfo.InvariantCulture), source.Address, issue.Code, source.OriginalValue, source.Value, source.NumberFormat, issue.SeverityLabel);
        }
    }

    public static string Csv(string? text)
    {
        text ??= "";
        var trimmed = text.AsSpan().TrimStart();
        if (text.Length > 0 && (text[0] is '\t' or '\r' or '\n' || trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@'))
            text = "'" + text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}
