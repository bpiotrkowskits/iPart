using iPart.Core.Components;
using iPart.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace iPart.Tests;

public sealed class InfrastructureModelTests
{
    internal static IPartDbContext CreateContext(params IInterceptor[] interceptors) => new(
        new DbContextOptionsBuilder<IPartDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=True;Encrypt=True")
            .AddInterceptors(interceptors).Options);

    [Fact]
    public void BusinessKeyIsUniqueUnfilteredAndBinaryCaseSensitive()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Component))!;
        var key = entity.GetIndexes().Single(x => x.GetDatabaseName() == "UX_Components_BusinessKey");
        Assert.True(key.IsUnique);
        Assert.Null(key.GetFilter());
        Assert.Equal(new[] { "Mpn", "ManufacturerId", "CustomerId" }, key.Properties.Select(x => x.Name));
        Assert.Equal("Latin1_General_100_BIN2", entity.FindProperty("Mpn")!.GetCollation());
        Assert.True(entity.FindProperty("ManufacturerId")!.IsNullable);
        Assert.True(entity.FindProperty("CustomerId")!.IsNullable);
    }

    [Fact]
    public void EveryDecimalUsesItsOwnDeclaredPrecision()
    {
        using var context = CreateContext();
        foreach (var entity in context.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties().Where(x => (Nullable.GetUnderlyingType(x.ClrType) ?? x.ClrType) == typeof(decimal)))
        {
            var spec = (DecimalSpecAttribute)Attribute.GetCustomAttribute(property.PropertyInfo!, typeof(DecimalSpecAttribute))!;
            Assert.NotNull(spec);
            Assert.Equal(spec.Precision, property.GetPrecision());
            Assert.Equal(spec.Scale, property.GetScale());
        }
    }

    [Fact]
    public void SubcategoryUsesCompositeForeignKeyAndCheck()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Component))!;
        var fk = entity.GetForeignKeys().Single(x => x.PrincipalEntityType.ClrType == typeof(Subcategory));
        Assert.Equal(new[] { "SubcategoryId", "CategoryId" }, fk.Properties.Select(x => x.Name));
        Assert.Contains(entity.GetCheckConstraints(), x => x.Name == "CK_Components_SubcategoryRequiresCategory");
    }

    [Fact]
    public void GeneratedSqlContainsNullAwareIndexAndChecksWithoutDataCreationCalls()
    {
        using var context = CreateContext();
        var sql = context.Database.GenerateCreateScript();
        var uniqueIndex = sql.Split('\n').Single(x => x.Contains("CREATE UNIQUE INDEX [UX_Components_BusinessKey]"));
        Assert.DoesNotContain("WHERE", uniqueIndex);
        Assert.Contains("CK_Capacitor_ToleranceAlternative", sql);
        Assert.Contains("CK_Pads_RC", sql);
        Assert.Contains("decimal(28,9)", sql);
        Assert.Contains("EsrMilliOhms", sql);
        Assert.DoesNotContain("EsrOhms", sql);
    }

    [Theory]
    [InlineData("parameters")] [InlineData("pads")] [InlineData("qualification")] [InlineData("link")]
    public void DependentOnlyEditUpdatesAggregateVersionBeforeSql(string kind)
    {
        using var context = CreateContext(new SuppressDatabaseWrite());
        var originalVersion = Guid.NewGuid();
        var component = new Component { Mpn = "test", Version = originalVersion, Resistor = new(), Pads = new() };
        component.Resistor.ComponentId = component.Id;
        component.Pads.ComponentId = component.Id;
        context.Attach(component);
        switch (kind)
        {
            case "parameters": component.Resistor.PowerWatts = .25m; break;
            case "pads": component.Pads.TP = 0; break;
            case "qualification": component.Qualifications.Add(new() { ComponentId = component.Id, QualificationId = 1 }); break;
            case "link": component.Links.Add(new() { ComponentId = component.Id, Kind = ComponentLinkKind.Documentation, Url = "https://example.org" }); break;
        }
        context.SaveChanges();
        Assert.NotEqual(originalVersion, component.Version);
        Assert.True(context.Entry(component).Property(x => x.Version).IsModified);
        Assert.Equal(originalVersion, context.Entry(component).Property(x => x.Version).OriginalValue);
        Assert.True(context.Entry(component).Property(x => x.Version).Metadata.IsConcurrencyToken);
    }

    [Fact]
    public async Task InvalidAggregateIsRejectedBeforeDatabaseAccess()
    {
        using var context = CreateContext(new SuppressDatabaseWrite());
        var component = new Component { Mpn = "test", Capacitor = new() { TolerancePercent = 1, AbsoluteTolerancePf = 2 } };
        component.Capacitor.ComponentId = component.Id;
        context.Add(component);
        await Assert.ThrowsAsync<ComponentValidationException>(() => context.SaveChangesAsync());
    }

    // These tests verify application preparation only; they are not SQL constraint/concurrency tests.
    private sealed class SuppressDatabaseWrite : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
            InterceptionResult<int>.SuppressWithResult(0);
    }
}
