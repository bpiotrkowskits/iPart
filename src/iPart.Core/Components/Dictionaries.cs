namespace iPart.Core.Components;

public interface INamedDictionary
{
    int Id { get; set; }
    string Name { get; set; }
}

public sealed class Manufacturer : INamedDictionary
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
public sealed class Category : INamedDictionary
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
public sealed class Subcategory : INamedDictionary
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int CategoryId { get; set; }
}
public sealed class MountingMethod : INamedDictionary
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Stały kod (ADR-0006), np. <c>THT</c>; nazwa wyświetlana może się zmieniać.</summary>
    public string Code { get; set; } = "";
}
public sealed class ProductStatus : INamedDictionary
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
public sealed class AecQualification : INamedDictionary
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
public sealed class CasePattern
{
    public int Id { get; set; }
    public string Pattern { get; set; } = "";
    public string? Description { get; set; }
    public string? Notes { get; set; }
}
