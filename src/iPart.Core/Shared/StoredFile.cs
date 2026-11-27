namespace iPart.Core.Shared;

/// <summary>
/// Plik, z którego powstały dane (ADR-0008): zapisywany przy imporcie, niezmienny.
/// Tabela w schemacie obszaru, który importuje plik.
/// </summary>
public abstract class StoredFile
{
    public const long MaxSizeBytes = 50L * 1024 * 1024;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = "";
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    /// <summary>SHA-256 zawartości, szesnastkowo małymi literami.</summary>
    public string Sha256 { get; set; } = "";
    public byte[] Content { get; set; } = [];
    public Guid? UploadedByUserId { get; set; }
    public string UploadedBy { get; set; } = "";
    public DateTimeOffset UploadedAtUtc { get; set; }

    public static string ComputeSha256(byte[] content) =>
        Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(content));
}

/// <summary>Pliki obszaru Dokumentacja pakietu (BOM, P&amp;P Klienta).</summary>
public sealed class DocumentationFile : StoredFile;
