using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace iPart.Core.Components;

public sealed record ComponentFieldChange(string Path, string? PreviousValue, string? NewValue);

public static class ComponentChanges
{
    public static IReadOnlyList<ComponentFieldChange> Between(Component? previous, Component current)
    {
        var before = Flatten(previous);
        var after = Flatten(current);
        return before.Keys.Union(after.Keys).Order(StringComparer.Ordinal).Select(path =>
        {
            before.TryGetValue(path, out var oldValue);
            after.TryGetValue(path, out var newValue);
            return new ComponentFieldChange(path, oldValue, newValue);
        }).Where(x => x.PreviousValue != x.NewValue).ToArray();
    }

    private static Dictionary<string, string?> Flatten(Component? component)
    {
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (component is null) return result;
        result["Component.Exists"] = "true";
        Scalars(component, "", result);
        foreach (var property in ComponentValidator.ParameterProperties)
        {
            if (property.GetValue(component) is not { } parameters) continue;
            result[property.Name + ".Exists"] = "true";
            Scalars(parameters, property.Name + ".", result);
        }
        foreach (var qualification in component.Qualifications)
            result[$"Qualifications.{qualification.QualificationId}"] = "true";
        foreach (var link in component.Links)
        {
            var prefix = $"Links.{link.Id:D}.";
            result[prefix + "Exists"] = "true";
            Scalars(link, prefix, result);
        }
        return result;
    }

    private static void Scalars(object value, string prefix, Dictionary<string, string?> result)
    {
        foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.Name is "Id" or "ComponentId" or "Version") continue;
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (type != typeof(string) && type != typeof(decimal) && type != typeof(int) && type != typeof(bool) && !type.IsEnum) continue;
            result[prefix + property.Name] = property.GetValue(value) switch
            {
                null => null,
                decimal number => number.ToString("G29", CultureInfo.InvariantCulture),
                var scalar => JsonSerializer.Serialize(scalar, type)
            };
        }
    }
}
