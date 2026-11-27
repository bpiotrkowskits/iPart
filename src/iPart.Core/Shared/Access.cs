using System.Text.Json;
using ProduMan.SharedKernel.Common;
using System.Text.Json.Serialization;

namespace iPart.Core.Shared;

/// <summary>Poziom uprawnienia (jak w ProduMan: podgląd, zapis). Brak uprawnienia = brak wpisu.</summary>
public enum PermissionLevel { View = 1, Edit = 2 }

/// <summary>Zakres działów uprawnienia (wzorzec ADR-0025 ProduMan).</summary>
public enum PermissionScope { Department = 1, Group = 2, All = 3 }

/// <summary>Uprawnienie z katalogu w kodzie — klucz, nazwa i dozwolone poziomy.</summary>
public sealed record PermissionDefinition(string Key, string Name, IReadOnlyList<PermissionLevel> Levels);

/// <summary>Nadane uprawnienie: poziom i zakres.</summary>
public sealed record PermissionGrant(PermissionLevel Level, PermissionScope Scope = PermissionScope.All);

/// <summary>
/// Katalog uprawnień iPart (ADR-0013). Kod sprawdza klucz uprawnienia — nigdy nazwę szablonu.
/// Ograniczenia z przypisów macierzy sesji 4 to osobne uprawnienia („tylko …”).
/// </summary>
public static class Permissions
{
    public const string DocumentationImport = "documentation.import";
    public const string DocumentationBom = "documentation.bom";
    public const string DocumentationBomSubstitutes = "documentation.bom.substitutes";
    public const string DocumentationBomApproval = "documentation.bom.approval";
    public const string DocumentationPnpApproval = "documentation.pnp.approval";
    public const string ComponentsBase = "components.base";
    public const string ComponentsFootprint = "components.footprint";
    public const string ComponentsSubstituteMpn = "components.substitute-mpn";
    public const string ComponentsStockRules = "components.stock-rules";
    public const string PurchasingSets = "purchasing.sets";
    public const string PurchasingSetsMultiZk = "purchasing.sets.multi-zk";
    public const string PurchasingPrices = "purchasing.prices";
    public const string Kitting = "kitting";
    public const string KittingProductionView = "kitting.production-view";
    public const string SharedAdminPanel = "shared.admin-panel";

    private static readonly PermissionLevel[] ViewEdit = [PermissionLevel.View, PermissionLevel.Edit];
    private static readonly PermissionLevel[] EditOnly = [PermissionLevel.Edit];

    public static readonly IReadOnlyList<PermissionDefinition> Catalog =
    [
        new(DocumentationImport, "Import i porównanie BOM / P&P", ViewEdit),
        new(DocumentationBom, "BOM pakietu (Part TS, MPN, zamienniki)", ViewEdit),
        new(DocumentationBomSubstitutes, "Tylko zamienniki w BOM, z podstawą", EditOnly),
        new(DocumentationBomApproval, "Zatwierdzenie BOM", EditOnly),
        new(DocumentationPnpApproval, "Zatwierdzenie P&P i Footprint", ViewEdit),
        new(ComponentsBase, "Baza komponentów", ViewEdit),
        new(ComponentsFootprint, "Tylko Footprint komponentu", EditOnly),
        new(ComponentsSubstituteMpn, "Tylko nowy MPN zamiennika (podstawowe dane)", EditOnly),
        new(ComponentsStockRules, "Reguły zapasu", ViewEdit),
        new(PurchasingSets, "Zestawienia zakupowe", ViewEdit),
        new(PurchasingSetsMultiZk, "Tylko zestawienia z kilkoma ZK (łączenie)", EditOnly),
        new(PurchasingPrices, "Ceny", ViewEdit),
        new(Kitting, "Kompletacja, partie, opakowania", ViewEdit),
        new(KittingProductionView, "Widok dla produkcji (dPart)", ViewEdit),
        new(SharedAdminPanel, "Panel administratora", EditOnly),
    ];

    public static PermissionDefinition? Find(string key) => Catalog.FirstOrDefault(p => p.Key == key);
}

/// <summary>
/// Uprawnienia jednego konta — mapa klucz → poziom i zakres, zapisywana jako JSON
/// (<c>{"purchasing.prices":{"level":"edit","scope":"all"}}</c>, wzorzec ADR-0025 ProduMan).
/// Klucz nieznany w katalogu albo niedozwolony poziom nie jest przyjmowany; brak klucza = brak dostępu.
/// </summary>
public sealed class AccountPermissions
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly SortedDictionary<string, PermissionGrant> _grants;

    private AccountPermissions(SortedDictionary<string, PermissionGrant> grants) => _grants = grants;

    public static AccountPermissions Empty => new(new(StringComparer.Ordinal));

    public IReadOnlyDictionary<string, PermissionGrant> Grants => _grants;

    public static Result<AccountPermissions> Create(IReadOnlyDictionary<string, PermissionGrant> grants)
    {
        var result = new SortedDictionary<string, PermissionGrant>(StringComparer.Ordinal);
        foreach (var (key, grant) in grants)
        {
            if (Permissions.Find(key) is not { } definition) return Result<AccountPermissions>.Fail($"Nieznane uprawnienie „{key}”.");
            if (!definition.Levels.Contains(grant.Level))
                return Result<AccountPermissions>.Fail($"Uprawnienie „{definition.Name}” nie ma poziomu „{Label(grant.Level)}”.");
            if (!Enum.IsDefined(grant.Scope)) return Result<AccountPermissions>.Fail($"Nieznany zakres uprawnienia „{definition.Name}”.");
            result[key] = grant;
        }
        return Result<AccountPermissions>.Ok(new(result));
    }

    /// <summary>Poziom uprawnienia albo <c>null</c>, gdy konto go nie ma.</summary>
    public PermissionLevel? LevelOf(string key) => _grants.TryGetValue(key, out var grant) ? grant.Level : null;

    public bool CanView(string key) => LevelOf(key) is not null;
    public bool CanEdit(string key) => LevelOf(key) == PermissionLevel.Edit;

    public string ToJson() => JsonSerializer.Serialize(_grants, Json);

    /// <summary>Odczyt z bazy: klucze spoza katalogu (np. po wycofaniu uprawnienia) są pomijane — brak = brak dostępu.</summary>
    public static AccountPermissions FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Empty;
        var stored = JsonSerializer.Deserialize<Dictionary<string, PermissionGrant>>(json, Json) ?? [];
        var known = stored.Where(x => Permissions.Find(x.Key) is { } d && d.Levels.Contains(x.Value.Level))
            .ToDictionary(x => x.Key, x => x.Value);
        return Create(known).Value!;
    }

    public static string Label(PermissionLevel level) => level == PermissionLevel.Edit ? "zapis" : "podgląd";
}

/// <summary>
/// Szablony uprawnień — kolumny macierzy z sesji 4 (ADR-0013). Używane tylko przy nadawaniu;
/// kod nigdy nie sprawdza, z jakiego szablonu pochodzi uprawnienie.
/// </summary>
public static class PermissionTemplates
{
    private const PermissionLevel V = PermissionLevel.View, E = PermissionLevel.Edit;

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, PermissionLevel>> All =
        new Dictionary<string, IReadOnlyDictionary<string, PermissionLevel>>(StringComparer.Ordinal)
        {
            ["Opiekun Projektu"] = Template(
                (Permissions.DocumentationImport, E), (Permissions.DocumentationBom, E), (Permissions.DocumentationBomApproval, E),
                (Permissions.ComponentsBase, E), (Permissions.ComponentsStockRules, V), (Permissions.PurchasingSets, V),
                (Permissions.PurchasingSetsMultiZk, E), (Permissions.Kitting, V), (Permissions.KittingProductionView, V)),
            ["Kierownik działu Obsługi Projektu"] = Template(
                (Permissions.DocumentationImport, E), (Permissions.DocumentationBom, E), (Permissions.DocumentationBomSubstitutes, E),
                (Permissions.DocumentationBomApproval, E), (Permissions.DocumentationPnpApproval, E), (Permissions.ComponentsBase, E),
                (Permissions.ComponentsFootprint, E), (Permissions.ComponentsSubstituteMpn, E), (Permissions.ComponentsStockRules, E),
                (Permissions.PurchasingSets, E), (Permissions.PurchasingSetsMultiZk, E), (Permissions.PurchasingPrices, V),
                (Permissions.Kitting, V), (Permissions.KittingProductionView, V), (Permissions.SharedAdminPanel, E)),
            ["Technolog SMT"] = Template(
                (Permissions.DocumentationImport, V), (Permissions.DocumentationBom, V), (Permissions.DocumentationPnpApproval, E),
                (Permissions.ComponentsBase, V), (Permissions.ComponentsFootprint, E), (Permissions.KittingProductionView, V)),
            ["Zakupowiec"] = Template(
                (Permissions.DocumentationImport, V), (Permissions.DocumentationBom, V), (Permissions.DocumentationBomSubstitutes, E),
                (Permissions.ComponentsBase, V), (Permissions.ComponentsSubstituteMpn, E), (Permissions.ComponentsStockRules, V),
                (Permissions.PurchasingSets, E), (Permissions.PurchasingSetsMultiZk, E), (Permissions.PurchasingPrices, E),
                (Permissions.Kitting, V), (Permissions.KittingProductionView, V)),
            ["Kierownik Logistyki"] = Template(
                (Permissions.DocumentationImport, V), (Permissions.DocumentationBom, V), (Permissions.ComponentsBase, V),
                (Permissions.ComponentsStockRules, E), (Permissions.PurchasingSets, E), (Permissions.PurchasingSetsMultiZk, E),
                (Permissions.PurchasingPrices, E), (Permissions.Kitting, V), (Permissions.KittingProductionView, V)),
            ["Magazyn"] = Template(
                (Permissions.DocumentationBom, V), (Permissions.ComponentsBase, V), (Permissions.PurchasingSets, V),
                (Permissions.Kitting, E), (Permissions.KittingProductionView, E)),
            ["Produkcja"] = Template(
                (Permissions.DocumentationBom, V), (Permissions.DocumentationPnpApproval, V), (Permissions.ComponentsBase, V),
                (Permissions.Kitting, V), (Permissions.KittingProductionView, V)),
            ["Opiekun klienta"] = Template((Permissions.DocumentationImport, V), (Permissions.DocumentationBom, V)),
        };

    /// <summary>Uprawnienia z szablonu z zakresem „wszystko” (żadna reguła iPart nie zależy jeszcze od działu).</summary>
    public static AccountPermissions Grant(string template) =>
        AccountPermissions.Create(All[template].ToDictionary(x => x.Key, x => new PermissionGrant(x.Value))).Value!;

    private static IReadOnlyDictionary<string, PermissionLevel> Template(params (string Key, PermissionLevel Level)[] items) =>
        items.ToDictionary(x => x.Key, x => x.Level, StringComparer.Ordinal);
}

/// <summary>Odmowa dostępu — serwer zamienia ją na odpowiedź 403 z czytelnym komunikatem.</summary>
public sealed class AccessDeniedException(string message) : Exception(message);
