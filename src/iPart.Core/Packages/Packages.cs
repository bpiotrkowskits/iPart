using iPart.Core.Components;
using iPart.Core.Shared;

namespace iPart.Core.Packages;

// Pakiet Elektroniczny: rozpoznawany po kodzie i rewizji Klienta (np. "PCBA1" rev "C").
public sealed class Package
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string CustomerRevision { get; set; } = "";
    public Guid? CustomerId { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = "";
}

// Kolejny BOM otrzymany dla tej samej rewizji Klienta ("Rewizja Importu" z iPart).
public sealed class BomRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PackageId { get; set; }
    public int Number { get; set; }
    public string SourceFileName { get; set; } = "";
    public string? SourceSheet { get; set; }
    // Kopia pliku BOM w bazie (ADR-0008); puste dla rewizji zapisanych przed etapem 1.
    public Guid? SourceFileId { get; set; }
    // P&P Klienta: kopia pliku i zatwierdzenie przez Technologa SMT (2.7, 5.4).
    public Guid? PnpFileId { get; set; }
    public string? PnpFileName { get; set; }
    public Guid? PnpApprovedByUserId { get; set; }
    public string? PnpApprovedBy { get; set; }
    public DateTimeOffset? PnpApprovedAtUtc { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
    public string ImportedBy { get; set; } = "";
    public string? Comment { get; set; }
    public List<BomLine> Lines { get; set; } = [];
}

// Linia BOM. LineNumber to stały numer linii — odpowiednik reguły "nie zmieniamy kolejności linii".
public sealed class BomLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BomRevisionId { get; set; }
    public int LineNumber { get; set; }
    public string? Designators { get; set; }
    public int? Quantity { get; set; }
    public string? PartTs { get; set; }
    public string? Footprint { get; set; }
    public string? Description { get; set; }
    public string? Value { get; set; }
    public string? PartNumber { get; set; }
    public string? Notes { get; set; }
    public bool Confirmed { get; set; }
    // Potwierdzenie linii wymaga podstawy (1.5): kod podstawy, uzasadnienie, kto (Guid konta i nazwa) i kiedy.
    public string? ConfirmationBasis { get; set; }
    public string? ConfirmationNote { get; set; }
    public Guid? ConfirmedByUserId { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
    // Sekcja TS: sposób montażu (kod słownika, np. THT). Part TS — pole PartTs.
    public string? MountingCode { get; set; }
    // Po porównaniu z P&P: decyzja (SPRAWDZONE / WYKAZ_ROZNIC), pytanie do Klienta i odpowiedź Klienta.
    public string? ComparisonDecision { get; set; }
    public string? DifferenceQuestion { get; set; }
    public string? CustomerAnswer { get; set; }
    public List<BomLineDesignator> ExpandedDesignators { get; set; } = [];
    // Kolumny Klienta bez przypisanego pola (dawniej Column1…Column15) — bez limitu liczby.
    public List<BomLineExtraValue> ExtraValues { get; set; } = [];
}

public sealed class BomLineDesignator
{
    public Guid BomLineId { get; set; }
    public Guid BomRevisionId { get; set; }
    public string Designator { get; set; } = "";
}

public sealed class BomLineExtraValue
{
    public Guid BomLineId { get; set; }
    public int Ordinal { get; set; }
    public string? Header { get; set; }
    public string Value { get; set; } = "";
}

public sealed record PackageSummary(Guid Id, string Code, string CustomerRevision, string? CustomerName,
    string? Description, int RevisionCount, DateTimeOffset CreatedAtUtc, string CreatedBy)
{
    public string DisplayName => $"{Code} rev {CustomerRevision}" + (CustomerName is null ? "" : $" — {CustomerName}");
}

public sealed record BomRevisionSummary(Guid Id, int Number, string SourceFileName, string? SourceSheet,
    DateTimeOffset ImportedAtUtc, string ImportedBy, int LineCount, string? Comment)
{
    public string DisplayName => $"Rewizja importu {Number} — {ImportedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}";
}

public sealed record NewPackageRequest(string Code, string CustomerRevision, Guid? CustomerId, string? Description);

public sealed class PackageOperationException(string message) : Exception(message);

public interface IPackageService
{
    Task<IReadOnlyList<PackageSummary>> ListPackagesAsync(string? search, CancellationToken cancellationToken = default);
    Task<Guid> CreatePackageAsync(NewPackageRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BomRevisionSummary>> ListRevisionsAsync(Guid packageId, CancellationToken cancellationToken = default);
    // sourceFileContent: zawartość importowanego pliku — zapisywana w bazie razem z rewizją (ADR-0008).
    Task<BomRevisionSummary> SaveBomRevisionAsync(Guid packageId, BomImportResult import, string sourceFileName,
        string? sourceSheet, string? comment, byte[] sourceFileContent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BomLine>> GetLinesAsync(Guid revisionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> ListCustomersAsync(CancellationToken cancellationToken = default);
    Task<Guid> AddCustomerAsync(string name, CancellationToken cancellationToken = default);
}

public sealed record ComponentListItem(string Mpn, string? Mounting, string? CaseName, string? Footprint, string? Notes)
{
    public bool IsNonSmtMounting => Components.MountingCategories.IsNonSmt(Mounting);
}

public interface IComponentBrowser
{
    Task<(IReadOnlyList<ComponentListItem> Items, int Total)> SearchAsync(string? mpnFragment, int take, CancellationToken cancellationToken = default);
}

public sealed record DatabaseInfo(string Server, string Database, string Authentication, bool Encrypt, bool TrustServerCertificate,
    IReadOnlyList<string> AppliedMigrations, IReadOnlyList<string> PendingMigrations);

public interface IDatabaseInfoReader
{
    Task<DatabaseInfo> ReadAsync(CancellationToken cancellationToken = default);
}

/// <summary>Podstawa potwierdzenia linii (1.5) — stałe kody zapisywane w bazie.</summary>
public static class ConfirmationBases
{
    public const string CustomerAnswer = "ODPOWIEDZ_KLIENTA";
    public const string PreviousRealization = "WCZESNIEJSZA_REALIZACJA";
    public const string CoordinatorDecision = "DECYZJA_OPIEKUNA";

    public static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [CustomerAnswer] = "Odpowiedź Klienta",
        [PreviousRealization] = "Wcześniejsza realizacja",
        [CoordinatorDecision] = "Decyzja Opiekuna Projektu (z uzasadnieniem)",
    };

    public static string Describe(string? code) => code is not null && Names.TryGetValue(code, out var name) ? name : code ?? "";
}

/// <summary>Komenda: potwierdź linię BOM z podstawą (1.5). Decyzja Opiekuna wymaga uzasadnienia.</summary>
public sealed record ConfirmBomLine(Guid LineId, string Basis, string? Note);

/// <summary>Handler komendy — ViewModel wywołuje go przez ten interfejs, nigdy bazę (ADR-0014).</summary>
public interface IConfirmBomLineHandler
{
    Task<ProduMan.SharedKernel.Common.Result> HandleAsync(ConfirmBomLine command, CancellationToken cancellationToken = default);
}

/// <summary>Wpis historii pakietu do wyświetlenia: zestaw zmian z listą pól „było → będzie”.</summary>
public sealed record HistoryEntry(DateTimeOffset OccurredAtUtc, string Author, string Operation, string? Comment, IReadOnlyList<string> Changes)
{
    public string When => OccurredAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
    public string Summary => string.Join(Environment.NewLine, Changes);
}

/// <summary>Serwis odczytu historii pakietu (zestawy zmian z obszaru Dokumentacja pakietu).</summary>
public interface IPackageHistoryReader
{
    Task<IReadOnlyList<HistoryEntry>> ListAsync(Guid packageId, CancellationToken cancellationToken = default);
}
