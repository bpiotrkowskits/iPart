using iPart.Core.Components;
using System.Text.Json.Serialization;

namespace iPart.Core.Import;

public enum ImportSeverity { Information, Warning, Error }
public enum ExcelCellKind { Text, Number, Boolean, Error }

public sealed class SourceCell
{
    private string _originalValue = "";
    public required string Sheet { get; init; }
    public required int Row { get; init; }
    public required int Column { get; init; }
    public required string Address { get; init; }
    public string? Header { get; set; }
    public required string OriginalValue { get => Formula is null ? _originalValue : "=" + Formula; init => _originalValue = value; }
    public string? Value { get; init; }
    public string? Formula { get; init; }
    public string? NumberFormat { get; init; }
    public ExcelCellKind Kind { get; init; }
    public string? Mapping { get; set; }
    public bool HasContent => !string.IsNullOrWhiteSpace(Value) || Formula is not null;
    public string Location => $"{Sheet}!{Address}";
}

public sealed record SourceRow(string Sheet, int Number, IReadOnlyList<SourceCell> Cells, bool IsPreparedFormulaRow = false);
public sealed class SourceSheet
{
    public required string Name { get; init; }
    public int HeaderRow { get; set; }
    public Dictionary<int, string> Headers { get; } = [];
    public List<SourceRow> Rows { get; } = [];
    public List<SourceCell> HeaderCells { get; } = [];
    public int IgnoredFormulaRows { get; set; }
    public int DataRows => Rows.Count(x => !x.IsPreparedFormulaRow);
    public string Purpose => Name switch
    {
        "partsData" => "Komponenty", "resValue" or "capValue" or "ctceValue" => "Parametry pomocnicze",
        "RMCase_source" or "standardCase" => "Tylko podgląd źródła", _ => "Arkusz bez ustalonego mapowania"
    };
}

public sealed record ImportIssue(ImportSeverity Severity, string Code, string Message,
    IReadOnlyList<SourceCell> Sources, int? PartRow = null)
{
    public string SeverityLabel => Severity switch { ImportSeverity.Error => "Błąd", ImportSeverity.Warning => "Ostrzeżenie", _ => "Informacja" };
    public string Locations => string.Join(", ", Sources.Take(8).Select(x => x.Location)) + (Sources.Count > 8 ? $" (+{Sources.Count - 8} źródeł w raporcie)" : "");
}

public sealed class ImportPreviewRow
{
    public required int SourceRow { get; init; }
    public required Component Component { get; init; }
    public string Mpn => Component.Mpn;
    public string? Mounting { get; set; }
    public string? SourceValue { get; set; }
    public string? GeneratedValue { get; set; }
    public List<ImportIssue> Issues { get; } = [];
    public Dictionary<string, List<SourceCell>> FieldSources { get; } = new(StringComparer.Ordinal);
    public List<SourceCell> Sources { get; } = [];
    public bool HasErrors => Issues.Any(x => x.Severity == ImportSeverity.Error);
    public bool HasWarnings => Issues.Any(x => x.Severity == ImportSeverity.Warning);
    public string Status => HasErrors ? "Błędy blokujące" : HasWarnings ? "Gotowy z ostrzeżeniami" : "Gotowy";
}

public sealed record ImportOptions(bool ConfirmThPToTp = false, bool ConfirmNonSToNs = false);
public sealed record ImportProgress(string Stage, string? Sheet, int RowsRead, int Percent);
public sealed record ImportBusinessKey(string Mpn, int? ManufacturerId, int? CustomerId);
public sealed record ImportReferences(IReadOnlyDictionary<string, int> MountingMethods,
    IReadOnlyDictionary<string, int> AecQualifications, IReadOnlySet<ImportBusinessKey> ExistingKeys);

// Deliberately exposes no mutation capability.
public interface IImportReferenceReader
{
    Task<ImportReferences> ReadAsync(CancellationToken cancellationToken);
}
public interface IExcelPreviewService
{
    Task<ImportReport> AnalyzeAsync(string path, ImportOptions options, IProgress<ImportProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class ImportReport
{
    public required string FilePath { get; init; }
    public required string InputSha256 { get; init; }
    public DateTimeOffset AnalyzedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public required ImportOptions Options { get; init; }
    public List<SourceSheet> Sheets { get; } = [];
    public List<ImportPreviewRow> Records { get; } = [];
    public List<ImportIssue> Issues { get; } = [];
    public List<string> UnmappedColumns { get; } = [];
    public int DuplicateGroups { get; set; }
    public int DatabaseCollisions { get; set; }
    public int UnmatchedAuxiliaryRows { get; set; }
    public int PartsDataRecords => Records.Count;
    // Ready includes warnings. Ready + Blocked = PartsDataRecords; warnings can overlap either group.
    public int ReadyRecords => Records.Count(x => !x.HasErrors);
    public int WarningRecords => Records.Count(x => x.HasWarnings);
    public int BlockedRecords => Records.Count(x => x.HasErrors);
    public int ProblemCount => Issues.Count;
    public int AffectedRecords => Records.Count(x => x.Issues.Any(i => i.Severity != ImportSeverity.Information));
    [JsonIgnore] public IEnumerable<SourceCell> AllSourceCells => Sheets.SelectMany(s => s.HeaderCells.Concat(s.Rows.SelectMany(r => r.Cells)));
}
