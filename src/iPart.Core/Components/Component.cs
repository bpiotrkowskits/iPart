namespace iPart.Core.Components;

public sealed class Component
{
    private string _mpn = "";
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Mpn { get => _mpn; set => _mpn = MpnRules.Normalize(value); }
    public Guid Version { get; set; }
    public int? ManufacturerId { get; set; }
    public string? NamePl { get; set; }
    public string? NameEng { get; set; }
    public string? DistributorName { get; set; }
    public string? CustomerComponentName { get; set; }
    public int? CategoryId { get; set; }
    public int? SubcategoryId { get; set; }
    public int? MountingMethodId { get; set; }
    public string? CaseName { get; set; }
    public int? CasePatternId { get; set; }
    public string? Footprint { get; set; }
    public string? Msl { get; set; }
    public bool? Rohs { get; set; }
    public int? ProductStatusId { get; set; }
    [DecimalSpec(9, 3, AllowNegative = true)] public decimal? MinimumOperatingTemperatureC { get; set; }
    [DecimalSpec(9, 3, AllowNegative = true)] public decimal? MaximumOperatingTemperatureC { get; set; }
    public string? Notes { get; set; }
    public bool? Critical { get; set; }
    public string? AdditionalDescription { get; set; }
    public string? LabelDescription { get; set; }
    public ResistorParameters? Resistor { get; set; }
    public CapacitorParameters? Capacitor { get; set; }
    public InductorParameters? Inductor { get; set; }
    public LedParameters? Led { get; set; }
    public ConnectorParameters? Connector { get; set; }
    public CrystalParameters? Crystal { get; set; }
    public ComponentPads? Pads { get; set; }
    public List<ComponentQualification> Qualifications { get; set; } = [];
    public List<ComponentLink> Links { get; set; } = [];
}

public static class MpnRules
{
    public const int MaxLength = 200;
    // Same ordinal equality as the SQL BIN2 collation; whitespace only at the edges is trimmed.
    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Trim();
    }
    public static bool Equal(string left, string right) =>
        StringComparer.Ordinal.Equals(Normalize(left), Normalize(right));
}

public interface IComponentDependent { Guid ComponentId { get; set; } }

public sealed class ComponentQualification : IComponentDependent
{
    public Guid ComponentId { get; set; }
    public int QualificationId { get; set; }
}

public enum ComponentLinkKind { CatalogImage = 1, Documentation = 2 }

public sealed class ComponentLink : IComponentDependent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ComponentId { get; set; }
    public ComponentLinkKind Kind { get; set; }
    public string Url { get; set; } = "";
    public string? Description { get; set; }
}
