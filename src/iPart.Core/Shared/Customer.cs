namespace iPart.Core.Shared;

/// <summary>
/// Klient firmy — dane wspólne dla wszystkich obszarów (schemat <c>shared</c>).
/// Stały GUID (ADR-0006); powiązanie z kontrahentem Nexo dojdzie jako osobne pole.
/// </summary>
public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? Code { get; set; }
}
