using System.Reflection;

namespace iPart.Core.Components;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DecimalSpecAttribute(int precision, int scale) : Attribute
{
    public int Precision { get; } = precision;
    public int Scale { get; } = scale;
    public bool AllowNegative { get; set; }
}

public sealed class ComponentValidationException(string message) : Exception(message);

public static class DecimalRules
{
    public static void Validate(decimal value, int precision, int scale, string field, bool allowNegative = false)
    {
        decimal limit = 1;
        for (var i = 0; i < precision - scale; i++) limit *= 10;
        if ((!allowNegative && value < 0) || value <= -limit || value >= limit)
            throw new ComponentValidationException($"{field}: wartość poza dozwolonym zakresem decimal({precision},{scale}).");
        if (decimal.Round(value, scale, MidpointRounding.AwayFromZero) != value)
            throw new ComponentValidationException($"{field}: zbyt duża dokładność; dozwolone {scale} miejsc dziesiętnych. Wartość nie została zaokrąglona.");
    }
}

public static class ComponentValidator
{
    public static void Validate(Component component)
    {
        if (component.Id == Guid.Empty) Fail("Komponent wymaga technicznego Id.");
        if (string.IsNullOrWhiteSpace(component.Mpn) || component.Mpn.Length > MpnRules.MaxLength)
            Fail($"MPN jest wymagany i może zawierać do {MpnRules.MaxLength} znaków.");
        if (component.SubcategoryId.HasValue && !component.CategoryId.HasValue)
            Fail("Podkategoria wymaga kategorii.");
        if (component.MinimumOperatingTemperatureC > component.MaximumOperatingTemperatureC)
            Fail("Minimalna temperatura nie może przekraczać maksymalnej.");
        ValidateObject(component);
        foreach (var property in ParameterProperties)
            if (property.GetValue(component) is { } parameters) ValidateObject(parameters);
        if (component.Capacitor is { TolerancePercent: not null, AbsoluteTolerancePf: not null })
            Fail("Tolerancja kondensatora może być procentowa albo bezwzględna, ale nie obie jednocześnie.");
        if (component.Pads is { } pads) ValidatePads(pads);
        if (component.Capacitor?.LifetimeHours < 0 || component.Connector?.PositionCount < 0 || component.Connector?.RowCount < 0)
            Fail("Liczby godzin, pozycji i rzędów muszą być nieujemne.");
        if (component.Qualifications.Any(x => x.QualificationId <= 0) ||
            component.Qualifications.Select(x => x.QualificationId).Distinct().Count() != component.Qualifications.Count)
            Fail("Kwalifikacje muszą mieć poprawne, niepowtarzające się identyfikatory.");
        if (component.Links.Any(x => x.Id == Guid.Empty) || component.Links.Select(x => x.Id).Distinct().Count() != component.Links.Count)
            Fail("Odnośniki muszą mieć niepowtarzające się identyfikatory.");
        foreach (var link in component.Links)
        {
            if (!Enum.IsDefined(link.Kind) || !Uri.TryCreate(link.Url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "https" && uri.Scheme != "http"))
                Fail("Odnośnik wymaga rodzaju i poprawnego adresu HTTP lub HTTPS.");
            ValidateObject(link);
        }
    }

    // Explicit aggregate boundary; no configuration or credentials enter validation/audit.
    public static readonly PropertyInfo[] ParameterProperties = typeof(Component).GetProperties()
        .Where(p => typeof(IComponentDependent).IsAssignableFrom(p.PropertyType)).ToArray();

    public static void ValidateObject(object value)
    {
        foreach (var property in value.GetType().GetProperties())
        {
            if (property.GetCustomAttribute<DecimalSpecAttribute>() is { } spec && property.GetValue(value) is decimal number)
                DecimalRules.Validate(number, spec.Precision, spec.Scale, property.Name, spec.AllowNegative);
            if (property.GetValue(value) is string text && text.Length > TextLimit(property.Name))
                Fail($"{property.Name}: przekroczona maksymalna długość {TextLimit(property.Name)} znaków.");
        }
    }

    public static int TextLimit(string property) => property switch
    {
        nameof(Component.Mpn) => MpnRules.MaxLength,
        nameof(Component.Notes) or nameof(Component.AdditionalDescription) => 16000,
        nameof(ComponentLink.Url) => 2048,
        nameof(Component.LabelDescription) => 500,
        _ => 500
    };

    public static void ValidatePads(ComponentPads p)
    {
        if (p.RC is not null and not 0 and not 2 || p.TT is not null and not 0 and not 2)
            Fail("PADS RC i TT: dozwolone NULL, 0 albo 2.");
        if (p.DL is < 0 or 1 or 2 || p.QL is < 0 or 1 or 2 or 3 || p.BGA is < 0 or 1 or 2)
            Fail("PADS DL/BGA: 0 lub co najmniej 3; QL: 0 lub co najmniej 4.");
        if (p.TP < 0 || p.THT < 0 || p.PreTHT < 0 || p.PressFIT < 0 || p.MECH < 0)
            Fail("Liczby wyprowadzeń PADS nie mogą być ujemne.");
    }

    private static void Fail(string message) => throw new ComponentValidationException(message);
}
