using iPart.Core.Components;

namespace iPart.Core.Packages;

// Pakiet Elektroniczny: rozpoznawany po kodzie i rewizji Klienta (np. "PCBA1" rev "C").
public sealed class Package
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string CustomerRevision { get; set; } = "";
    public int? CustomerId { get; set; }
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

public sealed record NewPackageRequest(string Code, string CustomerRevision, int? CustomerId, string? Description);

public sealed class PackageOperationException(string message) : Exception(message);

public interface IPackageService
{
    Task<IReadOnlyList<PackageSummary>> ListPackagesAsync(string? search, CancellationToken cancellationToken = default);
    Task<Guid> CreatePackageAsync(NewPackageRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BomRevisionSummary>> ListRevisionsAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task<BomRevisionSummary> SaveBomRevisionAsync(Guid packageId, BomImportResult import, string sourceFileName,
        string? sourceSheet, string? comment, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BomLine>> GetLinesAsync(Guid revisionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> ListCustomersAsync(CancellationToken cancellationToken = default);
    Task<int> AddCustomerAsync(string name, CancellationToken cancellationToken = default);
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
