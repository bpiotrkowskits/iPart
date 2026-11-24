using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using iPart.Core.Components;
using iPart.Core.Packages;

namespace iPart.Infrastructure;

public sealed class IPartDbContext(DbContextOptions<IPartDbContext> options) : DbContext(options)
{
    public DbSet<Component> Components => Set<Component>();
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<MountingMethod> MountingMethods => Set<MountingMethod>();
    public DbSet<ProductStatus> ProductStatuses => Set<ProductStatus>();
    public DbSet<AecQualification> AecQualifications => Set<AecQualification>();
    public DbSet<CasePattern> CasePatterns => Set<CasePattern>();
    public DbSet<ComponentHistoryEntry> ComponentHistory => Set<ComponentHistoryEntry>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<BomRevision> BomRevisions => Set<BomRevision>();
    public DbSet<BomLine> BomLines => Set<BomLine>();
    public DbSet<BomLineDesignator> BomLineDesignators => Set<BomLineDesignator>();
    public DbSet<BomLineExtraValue> BomLineExtraValues => Set<BomLineExtraValue>();

    public const string Schema = "dataPart";

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasDefaultSchema(Schema);

        var component = model.Entity<Component>();
        component.ToTable("Components", table =>
        {
            // Matches char.IsWhiteSpace / string.Trim in .NET; forbid unnormalized direct SQL writes.
            const string whitespace = "9,10,11,12,13,32,133,160,5760,8192,8193,8194,8195,8196,8197,8198,8199,8200,8201,8202,8232,8233,8239,8287,12288";
            table.HasCheckConstraint("CK_Components_MpnNormalized", $"DATALENGTH([Mpn]) > 0 AND UNICODE(LEFT([Mpn], 1)) NOT IN ({whitespace}) AND UNICODE(RIGHT([Mpn], 1)) NOT IN ({whitespace})");
            table.HasCheckConstraint("CK_Components_SubcategoryRequiresCategory", "[SubcategoryId] IS NULL OR [CategoryId] IS NOT NULL");
            table.HasCheckConstraint("CK_Components_TemperatureOrder", "[MinimumOperatingTemperatureC] IS NULL OR [MaximumOperatingTemperatureC] IS NULL OR [MinimumOperatingTemperatureC] <= [MaximumOperatingTemperatureC]");
        });
        component.HasKey(x => x.Id);
        component.Property(x => x.Id).ValueGeneratedNever();
        component.Property(x => x.Mpn).IsRequired().UseCollation("Latin1_General_100_BIN2")
            .UsePropertyAccessMode(PropertyAccessMode.Property);
        component.Property(x => x.Version).IsConcurrencyToken().ValueGeneratedNever();
        component.HasIndex(x => new { x.Mpn, x.ManufacturerId, x.CustomerId })
            .IsUnique().HasFilter(null).HasDatabaseName("UX_Components_BusinessKey");
        component.HasOne<Manufacturer>().WithMany().HasForeignKey(x => x.ManufacturerId).OnDelete(DeleteBehavior.Restrict);
        component.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        component.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        component.HasOne<Subcategory>().WithMany().HasForeignKey(x => new { x.SubcategoryId, x.CategoryId })
            .HasPrincipalKey(x => new { x.Id, x.CategoryId }).OnDelete(DeleteBehavior.Restrict);
        component.HasOne<MountingMethod>().WithMany().HasForeignKey(x => x.MountingMethodId).OnDelete(DeleteBehavior.Restrict);
        component.HasOne<ProductStatus>().WithMany().HasForeignKey(x => x.ProductStatusId).OnDelete(DeleteBehavior.Restrict);
        component.HasOne<CasePattern>().WithMany().HasForeignKey(x => x.CasePatternId).OnDelete(DeleteBehavior.Restrict);

        Dictionary<Manufacturer>(model, "Manufacturers");
        Dictionary<Customer>(model, "Customers");
        Dictionary<Category>(model, "Categories");
        Dictionary<Subcategory>(model, "Subcategories");
        Dictionary<MountingMethod>(model, "MountingMethods");
        Dictionary<ProductStatus>(model, "ProductStatuses");
        Dictionary<AecQualification>(model, "AecQualifications");
        model.Entity<Subcategory>().HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<CasePattern>().ToTable("CasePatterns");
        model.Entity<CasePattern>().Property(x => x.Pattern).IsRequired();

        model.Entity<MountingMethod>().HasData(new MountingMethod { Id = 1, Name = "SMT" }, new MountingMethod { Id = 2, Name = "Pin-In-Paste" },
            new MountingMethod { Id = 3, Name = "THT" }, new MountingMethod { Id = 4, Name = "Press-Fit" }, new MountingMethod { Id = 5, Name = "Plugs" }, new MountingMethod { Id = 6, Name = "Mech" });
        model.Entity<ProductStatus>().HasData(new ProductStatus { Id = 1, Name = "Last Time Buy" }, new ProductStatus { Id = 2, Name = "Not For New Design" },
            new ProductStatus { Id = 3, Name = "Obsolete" }, new ProductStatus { Id = 4, Name = "End of Life" });
        model.Entity<AecQualification>().HasData(new AecQualification { Id = 1, Name = "AEC-Q200" }, new AecQualification { Id = 2, Name = "AEC-Q101" }, new AecQualification { Id = 3, Name = "AEC-Q100" });

        Parameters(model, x => x.Resistor, "ResistorParameters");
        Parameters(model, x => x.Capacitor, "CapacitorParameters");
        Parameters(model, x => x.Inductor, "InductorParameters");
        Parameters(model, x => x.Led, "LedParameters");
        Parameters(model, x => x.Connector, "ConnectorParameters");
        Parameters(model, x => x.Crystal, "CrystalParameters");
        Parameters(model, x => x.Pads, "ComponentPads");
        model.Entity<CapacitorParameters>().ToTable("CapacitorParameters", t =>
        {
            t.HasCheckConstraint("CK_Capacitor_ToleranceAlternative", "[TolerancePercent] IS NULL OR [AbsoluteTolerancePf] IS NULL");
            t.HasCheckConstraint("CK_Capacitor_Lifetime", "[LifetimeHours] >= 0");
        });
        model.Entity<ConnectorParameters>().ToTable("ConnectorParameters", t =>
        {
            t.HasCheckConstraint("CK_Connector_PositionCount", "[PositionCount] >= 0");
            t.HasCheckConstraint("CK_Connector_RowCount", "[RowCount] >= 0");
        });
        model.Entity<ComponentPads>().ToTable("ComponentPads", t =>
        {
            t.HasCheckConstraint("CK_Pads_RC", "[RC] IN (0, 2)");
            t.HasCheckConstraint("CK_Pads_TT", "[TT] IN (0, 2)");
            t.HasCheckConstraint("CK_Pads_DL", "[DL] = 0 OR [DL] >= 3");
            t.HasCheckConstraint("CK_Pads_QL", "[QL] = 0 OR [QL] >= 4");
            t.HasCheckConstraint("CK_Pads_BGA", "[BGA] = 0 OR [BGA] >= 3");
            foreach (var name in new[] { "TP", "THT", "PreTHT", "PressFIT", "MECH" })
                t.HasCheckConstraint("CK_Pads_" + name, $"[{name}] >= 0");
        });

        var qualification = model.Entity<ComponentQualification>();
        qualification.ToTable("ComponentQualifications");
        qualification.HasKey(x => new { x.ComponentId, x.QualificationId });
        component.HasMany(x => x.Qualifications).WithOne().HasForeignKey(x => x.ComponentId).OnDelete(DeleteBehavior.Cascade);
        qualification.HasOne<AecQualification>().WithMany().HasForeignKey(x => x.QualificationId).OnDelete(DeleteBehavior.Restrict);
        var link = model.Entity<ComponentLink>();
        link.ToTable("ComponentLinks", t => t.HasCheckConstraint("CK_ComponentLinks_Kind", "[Kind] IN (1, 2)"));
        link.HasKey(x => x.Id);
        link.Property(x => x.Id).ValueGeneratedNever();
        component.HasMany(x => x.Links).WithOne().HasForeignKey(x => x.ComponentId).OnDelete(DeleteBehavior.Cascade);
        var history = model.Entity<ComponentHistoryEntry>();
        history.ToTable("ComponentHistory");
        history.HasKey(x => x.Id);
        history.HasOne<Component>().WithMany().HasForeignKey(x => x.ComponentId).OnDelete(DeleteBehavior.Restrict);
        history.HasIndex(x => new { x.ComponentId, x.OccurredAtUtc });
        history.HasIndex(x => x.OperationId);
        history.Property(x => x.Author).HasMaxLength(256).IsRequired();
        history.Property(x => x.FieldPath).HasMaxLength(512).IsRequired();
        history.Property(x => x.PreviousValue).HasColumnType("nvarchar(max)");
        history.Property(x => x.NewValue).HasColumnType("nvarchar(max)");

        foreach (var entity in model.Model.GetEntityTypes().Where(e => e.ClrType != typeof(ComponentHistoryEntry)))
        {
            foreach (var property in entity.ClrType.GetProperties())
            {
                if (property.PropertyType == typeof(string))
                    model.Entity(entity.ClrType).Property(property.Name).HasMaxLength(ComponentValidator.TextLimit(property.Name));
                if (property.GetCustomAttribute<DecimalSpecAttribute>() is not { } spec) continue;
                model.Entity(entity.ClrType).Property(property.Name).HasPrecision(spec.Precision, spec.Scale);
                if (!spec.AllowNegative)
                    model.Entity(entity.ClrType).ToTable(entity.GetTableName()!, t =>
                        t.HasCheckConstraint($"CK_{entity.GetTableName()}_{property.Name}", $"[{property.Name}] >= 0"));
            }
        }
        model.Entity<ResistorParameters>().HasIndex(x => x.ResistanceOhms);
        model.Entity<ResistorParameters>().HasIndex(x => x.PowerWatts);
        model.Entity<ResistorParameters>().HasIndex(x => x.TolerancePercent);
        model.Entity<CapacitorParameters>().HasIndex(x => x.CapacitancePf);
        model.Entity<CapacitorParameters>().HasIndex(x => x.RatedVoltageV);
        model.Entity<InductorParameters>().HasIndex(x => x.InductanceMicroHenries);
        model.Entity<InductorParameters>().HasIndex(x => x.RatedCurrentA);
        model.Entity<LedParameters>().HasIndex(x => x.Color);
        model.Entity<ConnectorParameters>().HasIndex(x => x.PitchMm);
        model.Entity<ConnectorParameters>().HasIndex(x => x.PositionCount);
        model.Entity<CrystalParameters>().HasIndex(x => x.FrequencyHz);

        ConfigurePackages(model);
    }

    // Pakiety Elektroniczne i rewizje BOM. Konfiguracja po pętli długości tekstów, żeby ją nadpisać.
    private static void ConfigurePackages(ModelBuilder model)
    {
        var package = model.Entity<Package>();
        package.ToTable("Packages");
        package.HasKey(x => x.Id);
        package.Property(x => x.Id).ValueGeneratedNever();
        package.Property(x => x.Code).HasMaxLength(100).IsRequired();
        package.Property(x => x.CustomerRevision).HasMaxLength(50).IsRequired();
        package.Property(x => x.Description).HasMaxLength(1000);
        package.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
        package.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        package.HasIndex(x => new { x.Code, x.CustomerRevision, x.CustomerId }).IsUnique().HasFilter(null)
            .HasDatabaseName("UX_Packages_CodeRevisionCustomer");

        var revision = model.Entity<BomRevision>();
        revision.ToTable("BomRevisions", t => t.HasCheckConstraint("CK_BomRevisions_Number", "[Number] >= 1"));
        revision.HasKey(x => x.Id);
        revision.Property(x => x.Id).ValueGeneratedNever();
        revision.Property(x => x.SourceFileName).HasMaxLength(260).IsRequired();
        revision.Property(x => x.SourceSheet).HasMaxLength(100);
        revision.Property(x => x.ImportedBy).HasMaxLength(256).IsRequired();
        revision.Property(x => x.Comment).HasMaxLength(1000);
        revision.HasOne<Package>().WithMany().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Restrict);
        revision.HasIndex(x => new { x.PackageId, x.Number }).IsUnique();
        revision.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.BomRevisionId).OnDelete(DeleteBehavior.Cascade);

        var line = model.Entity<BomLine>();
        line.ToTable("BomLines", t =>
        {
            t.HasCheckConstraint("CK_BomLines_LineNumber", "[LineNumber] >= 1");
            t.HasCheckConstraint("CK_BomLines_Quantity", "[Quantity] >= 0");
        });
        line.HasKey(x => x.Id);
        line.Property(x => x.Id).ValueGeneratedNever();
        line.Property(x => x.Designators).HasColumnType("nvarchar(max)");
        line.Property(x => x.PartTs).HasMaxLength(500);
        line.Property(x => x.Footprint).HasMaxLength(500);
        line.Property(x => x.Description).HasMaxLength(2000);
        line.Property(x => x.Value).HasMaxLength(500);
        line.Property(x => x.PartNumber).HasMaxLength(1000);
        line.Property(x => x.Notes).HasMaxLength(4000);
        line.HasIndex(x => new { x.BomRevisionId, x.LineNumber }).IsUnique();
        line.HasMany(x => x.ExpandedDesignators).WithOne().HasForeignKey(x => x.BomLineId).OnDelete(DeleteBehavior.Cascade);
        line.HasMany(x => x.ExtraValues).WithOne().HasForeignKey(x => x.BomLineId).OnDelete(DeleteBehavior.Cascade);

        var designator = model.Entity<BomLineDesignator>();
        designator.ToTable("BomLineDesignators");
        designator.HasKey(x => new { x.BomLineId, x.Designator });
        designator.Property(x => x.Designator).HasMaxLength(100).UseCollation("Latin1_General_100_BIN2");
        // Jeden desygnator może wystąpić tylko raz w danej rewizji BOM.
        designator.HasIndex(x => new { x.BomRevisionId, x.Designator }).IsUnique();
        designator.HasOne<BomRevision>().WithMany().HasForeignKey(x => x.BomRevisionId).OnDelete(DeleteBehavior.NoAction);

        var extra = model.Entity<BomLineExtraValue>();
        extra.ToTable("BomLineExtraValues", t => t.HasCheckConstraint("CK_BomLineExtraValues_Ordinal", "[Ordinal] >= 1"));
        extra.HasKey(x => new { x.BomLineId, x.Ordinal });
        extra.Property(x => x.Header).HasMaxLength(500);
        extra.Property(x => x.Value).HasMaxLength(4000).IsRequired();
    }

    private static void Dictionary<T>(ModelBuilder model, string table) where T : class, INamedDictionary
    {
        var entity = model.Entity<T>();
        entity.ToTable(table);
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).IsRequired();
        entity.HasIndex(x => x.Name);
    }

    private static void Parameters<T>(ModelBuilder model, Expression<Func<Component, T?>> navigation, string table)
        where T : class, IComponentDependent
    {
        model.Entity<T>().ToTable(table);
        model.Entity<T>().HasKey(x => x.ComponentId);
        model.Entity<T>().Property(x => x.ComponentId).ValueGeneratedNever();
        model.Entity<Component>().HasOne(navigation).WithOne().HasForeignKey<T>(x => x.ComponentId).OnDelete(DeleteBehavior.Cascade);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareAggregateChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepareAggregateChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareAggregateChanges()
    {
        ChangeTracker.DetectChanges();
        var changed = ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        var roots = changed.Where(e => e.Entity is Component).Select(e => ((Component)e.Entity).Id)
            .Concat(changed.Where(e => e.Entity is IComponentDependent).Select(e => ((IComponentDependent)e.Entity).ComponentId)).Distinct();
        foreach (var id in roots)
        {
            var entry = ChangeTracker.Entries<Component>().SingleOrDefault(e => e.Entity.Id == id)
                ?? throw new InvalidOperationException("Zmiana parametrów wymaga wczytania całego komponentu i jego wersji.");
            if (entry.State == EntityState.Deleted) continue;
            ComponentValidator.Validate(entry.Entity);
            entry.Entity.Version = Guid.NewGuid();
            if (entry.State != EntityState.Added) entry.Property(x => x.Version).IsModified = true;
        }
    }
}
