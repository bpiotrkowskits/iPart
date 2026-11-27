using iPart.Core.Shared;

namespace iPart.Tests;

public sealed class PermissionsTests
{
    // Szablony w kodzie muszą odpowiadać tabeli „Katalog uprawnień i szablony” w karcie modułu (ADR-0013).
    [Fact]
    public void TemplatesMatchTableInModuleCard()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "iPart.slnx"))) root = root.Parent;
        var card = File.ReadAllLines(Path.Combine(root!.FullName, "docs/obszary/Usługi wspólne/moduly/Użytkownicy i dostępy.md"));
        var header = card.Single(l => l.StartsWith("| Uprawnienie | Klucz |", StringComparison.Ordinal));
        var templates = Cells(header).Skip(2).ToArray();
        Assert.Equal(new[] { "Opiekun Projektu", "Kier. działu OP", "Technolog SMT", "Zakupowiec", "Kier. Logistyki", "Magazyn", "Produkcja", "Opiekun klienta" }, templates);
        var codeNames = PermissionTemplates.All.Keys.ToArray();

        var rows = card.SkipWhile(l => l != header).Skip(2).TakeWhile(l => l.StartsWith('|')).Select(Cells).ToList();
        Assert.Equal(Permissions.Catalog.Select(p => p.Key), rows.Select(r => r[1].Trim('`')));
        foreach (var row in rows)
        {
            var key = row[1].Trim('`');
            for (var i = 0; i < templates.Length; i++)
            {
                PermissionLevel? expected = row[i + 2] switch { "Z" => PermissionLevel.Edit, "P" => PermissionLevel.View, "—" => null, var x => throw new InvalidDataException(x) };
                Assert.True(expected == PermissionTemplates.Grant(codeNames[i]).LevelOf(key), $"{codeNames[i]} / {key}");
            }
        }
    }

    [Fact]
    public void JsonRoundTripAndMissingKeyMeansNoAccess()
    {
        var granted = PermissionTemplates.Grant("Zakupowiec");
        Assert.True(granted.CanEdit(Permissions.PurchasingPrices));
        Assert.True(granted.CanView(Permissions.DocumentationBom));
        Assert.False(granted.CanEdit(Permissions.DocumentationBom));
        Assert.False(granted.CanView(Permissions.SharedAdminPanel));

        var json = granted.ToJson();
        Assert.Contains("\"purchasing.prices\":{\"level\":\"edit\",\"scope\":\"all\"}", json);
        Assert.Equal(granted.Grants, AccountPermissions.FromJson(json).Grants);
    }

    [Fact]
    public void UnknownKeyOrLevelIsRejectedOnGrantAndIgnoredOnRead()
    {
        Assert.False(AccountPermissions.Create(new Dictionary<string, PermissionGrant> { ["nie.ma"] = new(PermissionLevel.Edit) }).IsSuccess);
        var viewOnlyEditKey = AccountPermissions.Create(new Dictionary<string, PermissionGrant> { [Permissions.SharedAdminPanel] = new(PermissionLevel.View) });
        Assert.False(viewOnlyEditKey.IsSuccess);
        Assert.Contains("podgląd", viewOnlyEditKey.Error);

        var read = AccountPermissions.FromJson("""{"nie.ma":{"level":"edit","scope":"all"},"kitting":{"level":"view","scope":"department"}}""");
        Assert.Single(read.Grants);
        Assert.Equal(new PermissionGrant(PermissionLevel.View, PermissionScope.Department), read.Grants[Permissions.Kitting]);
        Assert.Empty(AccountPermissions.FromJson(null).Grants);
    }

    private static string[] Cells(string line) => line.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray();
}
