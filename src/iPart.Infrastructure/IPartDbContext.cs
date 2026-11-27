using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using iPart.Core.Components;
using iPart.Core.Packages;
using iPart.Core.Shared;

namespace iPart.Infrastructure;

public sealed class IPartDbContext(DbContextOptions<IPartDbContext> options) : DbContext(options)
{
    public DbSet<Component> Components => Set<Component>();
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<MountingMethod> MountingMethods => Set<MountingMethod>();
    public DbSet<ProductStatus> ProductStatuses => Set<ProductStatus>();
    public DbSet<AecQualification> AecQualifications => Set<AecQualification>();
    public DbSet<CasePattern> CasePatterns => Set<CasePattern>();
    public DbSet<ComponentsChangeSet> ComponentChangeSets => Set<ComponentsChangeSet>();
    public DbSet<ComponentsFieldChange> ComponentFieldChanges => Set<ComponentsFieldChange>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<BomRevision> BomRevisions => Set<BomRevision>();
    public DbSet<BomLine> BomLines => Set<BomLine>();
    public DbSet<BomLineDesignator> BomLineDesignators => Set<BomLineDesignator>();
    public DbSet<BomLineExtraValue> BomLineExtraValues => Set<BomLineExtraValue>();
    public DbSet<DocumentationFile> DocumentationFiles => Set<DocumentationFile>();
    public DbSet<PnpRow> PnpRows => Set<PnpRow>();
    public DbSet<LineMpn> LineMpns => Set<LineMpn>();
    public DbSet<BomVersion> BomVersions => Set<BomVersion>();
    public DbSet<DocumentationChangeSet> DocumentationChangeSets => Set<DocumentationChangeSet>();
    public DbSet<DocumentationFieldChange> DocumentationFieldChanges => Set<DocumentationFieldChange>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ProduManAccount> ProduManAccounts => Set<ProduManAccount>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<UserAccess> UserAccess => Set<UserAccess>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<SharedChangeSet> SharedChangeSets => Set<SharedChangeSet>();
    public DbSet<SharedFieldChange> SharedFieldChanges => Set<SharedFieldChange>();

    // Jeden schemat na obszar (ADR-0005). Obszar nie zapisuje do tabel innego obszaru;
    // między schematami tylko klucze obce do identyfikatorów.
    public const string ComponentsSchema = "components";
    public const string DocumentationSchema = "documentation";
    public const string SharedSchema = "shared";

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasDefaultSchema(ComponentsSchema);

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
        // Klucz komponentu: MPN + producent (10.5). Klient nie jest częścią klucza — własność fizyczna przy symbolu magazynowym.
        component.HasIndex(x => new { x.Mpn, x.ManufacturerId })
            .IsUnique().HasFilter(null).HasDatabaseName("UX_Components_BusinessKey");
        component.HasOne<Manufacturer>().WithMany().HasForeignKey(x => x.ManufacturerId).OnDelete(DeleteBehavior.Restrict);
        component.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        component.HasOne<Subcategory>().WithMany().HasForeignKey(x => new { x.SubcategoryId, x.CategoryId })
            .HasPrincipalKey(x => new { x.Id, x.CategoryId }).OnDelete(DeleteBehavior.Restrict);
        component.HasOne<MountingMethod>().WithMany().HasForeignKey(x => x.MountingMethodId).OnDelete(DeleteBehavior.Restrict);
        component.HasOne<ProductStatus>().WithMany().HasForeignKey(x => x.ProductStatusId).OnDelete(DeleteBehavior.Restrict);
        component.HasOne<CasePattern>().WithMany().HasForeignKey(x => x.CasePatternId).OnDelete(DeleteBehavior.Restrict);

        Dictionary<Manufacturer>(model, "Manufacturers");
        Dictionary<Category>(model, "Categories");
        Dictionary<Subcategory>(model, "Subcategories");
        Dictionary<MountingMethod>(model, "MountingMethods");
        Dictionary<ProductStatus>(model, "ProductStatuses");
        Dictionary<AecQualification>(model, "AecQualifications");
        model.Entity<Subcategory>().HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<CasePattern>().ToTable("CasePatterns");
        model.Entity<CasePattern>().Property(x => x.Pattern).IsRequired();

        // Sposoby montażu (5.6). Kod stały, nazwa do wyświetlania.
        model.Entity<MountingMethod>().HasData(
            new MountingMethod { Id = 1, Name = "SMT", Code = "SMT" }, new MountingMethod { Id = 2, Name = "Pin-In-Paste", Code = "PIN_IN_PASTE" },
            new MountingMethod { Id = 3, Name = "THT", Code = "THT" }, new MountingMethod { Id = 4, Name = "Press-Fit", Code = "PRESS_FIT" },
            new MountingMethod { Id = 5, Name = "Plugs", Code = "PLUGS" }, new MountingMethod { Id = 6, Name = "Mech", Code = "MECH" },
            new MountingMethod { Id = 7, Name = "PCB", Code = "PCB" }, new MountingMethod { Id = 8, Name = "DNP", Code = "DNP" });
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
        // Historia i pliki mają własne długości tekstów — poza pętlą limitów komponentu.
        foreach (var entity in model.Model.GetEntityTypes().Where(e => !typeof(ChangeSet).IsAssignableFrom(e.ClrType)
                     && !typeof(FieldChange).IsAssignableFrom(e.ClrType) && !typeof(StoredFile).IsAssignableFrom(e.ClrType)))
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

        model.Entity<MountingMethod>().Property(x => x.Code).HasMaxLength(50).IsRequired();
        model.Entity<MountingMethod>().HasIndex(x => x.Code).IsUnique();
        History<ComponentsChangeSet, ComponentsFieldChange>(model, ComponentsSchema, x => x.Changes);
        ConfigurePackages(model);
        ConfigureShared(model);
    }

    // Obszar Dokumentacja pakietu: pakiety, rewizje importu, linie, pliki źródłowe.
    // Konfiguracja po pętli długości tekstów, żeby ją nadpisać.
    private static void ConfigurePackages(ModelBuilder model)
    {
        var package = model.Entity<Package>();
        package.ToTable("Packages", DocumentationSchema);
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
        revision.ToTable("BomRevisions", DocumentationSchema, t => t.HasCheckConstraint("CK_BomRevisions_Number", "[Number] >= 1"));
        revision.HasKey(x => x.Id);
        revision.Property(x => x.Id).ValueGeneratedNever();
        revision.Property(x => x.SourceFileName).HasMaxLength(260).IsRequired();
        revision.Property(x => x.SourceSheet).HasMaxLength(100);
        revision.Property(x => x.ImportedBy).HasMaxLength(256).IsRequired();
        revision.Property(x => x.Comment).HasMaxLength(1000);
        revision.HasOne<Package>().WithMany().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Restrict);
        revision.HasIndex(x => new { x.PackageId, x.Number }).IsUnique();
        revision.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.BomRevisionId).OnDelete(DeleteBehavior.Cascade);
        revision.HasOne<DocumentationFile>().WithMany().HasForeignKey(x => x.SourceFileId).OnDelete(DeleteBehavior.Restrict);

        var line = model.Entity<BomLine>();
        line.ToTable("BomLines", DocumentationSchema, t =>
        {
            t.HasCheckConstraint("CK_BomLines_LineNumber", "[LineNumber] >= 1");
            t.HasCheckConstraint("CK_BomLines_Quantity", "[Quantity] >= 0");
            // Potwierdzona linia ma zawsze podstawę (1.5).
            t.HasCheckConstraint("CK_BomLines_ConfirmationBasis", "[Confirmed] = 0 OR [ConfirmationBasis] IS NOT NULL");
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
        line.Property(x => x.ConfirmationBasis).HasMaxLength(50).IsUnicode(false);
        line.Property(x => x.ConfirmationNote).HasMaxLength(1000);
        line.Property(x => x.ConfirmedBy).HasMaxLength(256);
        line.HasIndex(x => new { x.BomRevisionId, x.LineNumber }).IsUnique();
        line.HasMany(x => x.ExpandedDesignators).WithOne().HasForeignKey(x => x.BomLineId).OnDelete(DeleteBehavior.Cascade);
        line.HasMany(x => x.ExtraValues).WithOne().HasForeignKey(x => x.BomLineId).OnDelete(DeleteBehavior.Cascade);

        var designator = model.Entity<BomLineDesignator>();
        designator.ToTable("BomLineDesignators", DocumentationSchema);
        designator.HasKey(x => new { x.BomLineId, x.Designator });
        designator.Property(x => x.Designator).HasMaxLength(100).UseCollation("Latin1_General_100_BIN2");
        // Jeden desygnator może wystąpić tylko raz w danej rewizji BOM.
        designator.HasIndex(x => new { x.BomRevisionId, x.Designator }).IsUnique();
        designator.HasOne<BomRevision>().WithMany().HasForeignKey(x => x.BomRevisionId).OnDelete(DeleteBehavior.NoAction);

        var extra = model.Entity<BomLineExtraValue>();
        extra.ToTable("BomLineExtraValues", DocumentationSchema, t => t.HasCheckConstraint("CK_BomLineExtraValues_Ordinal", "[Ordinal] >= 1"));
        extra.HasKey(x => new { x.BomLineId, x.Ordinal });
        extra.Property(x => x.Header).HasMaxLength(500);
        extra.Property(x => x.Value).HasMaxLength(4000).IsRequired();

        // P&P Klienta przy rewizji importu (oryginał, niezmienny).
        var pnp = model.Entity<PnpRow>();
        pnp.ToTable("PnpRows", DocumentationSchema);
        pnp.HasKey(x => x.Id);
        pnp.Property(x => x.Id).ValueGeneratedNever();
        pnp.Property(x => x.Designator).HasMaxLength(100).IsRequired();
        foreach (var name in new[] { nameof(PnpRow.X), nameof(PnpRow.Y), nameof(PnpRow.Rotation), nameof(PnpRow.Side) }) pnp.Property(name).HasMaxLength(50);
        pnp.Property(x => x.Footprint).HasMaxLength(500);
        pnp.Property(x => x.Value).HasMaxLength(500);
        pnp.HasIndex(x => new { x.BomRevisionId, x.Designator }).IsUnique();
        pnp.HasOne<BomRevision>().WithMany().HasForeignKey(x => x.BomRevisionId).OnDelete(DeleteBehavior.Cascade);
        revision.HasOne<DocumentationFile>().WithMany().HasForeignKey(x => x.PnpFileId).OnDelete(DeleteBehavior.Restrict);
        revision.Property(x => x.PnpFileName).HasMaxLength(260);
        revision.Property(x => x.PnpApprovedBy).HasMaxLength(256);

        // MPN zatwierdzone i niedozwolone linii.
        var mpn = model.Entity<LineMpn>();
        mpn.ToTable("LineMpns", DocumentationSchema);
        mpn.HasKey(x => x.Id);
        mpn.Property(x => x.Id).ValueGeneratedNever();
        mpn.Property(x => x.Mpn).HasMaxLength(200).IsRequired();
        mpn.Property(x => x.Note).HasMaxLength(1000).IsRequired();
        mpn.Property(x => x.AddedBy).HasMaxLength(256).IsRequired();
        mpn.HasIndex(x => new { x.BomLineId, x.Mpn, x.Forbidden }).IsUnique();
        mpn.HasOne<BomLine>().WithMany().HasForeignKey(x => x.BomLineId).OnDelete(DeleteBehavior.Cascade);

        // Zatwierdzone wersje BOM — niezmienne, pełna kopia linii.
        var version = model.Entity<BomVersion>();
        version.ToTable("BomVersions", DocumentationSchema, t => t.HasCheckConstraint("CK_BomVersions_Kind", "[Kind] IN ('BAZOWA', 'DLA_ZK') AND ([Kind] = 'BAZOWA' OR [OrderNumber] IS NOT NULL)"));
        version.HasKey(x => x.Id);
        version.Property(x => x.Id).ValueGeneratedNever();
        version.Property(x => x.Kind).HasMaxLength(20).IsUnicode(false);
        version.Property(x => x.OrderNumber).HasMaxLength(20);
        version.Property(x => x.Comment).HasMaxLength(4000);
        version.Property(x => x.ApprovedBy).HasMaxLength(256).IsRequired();
        version.Property(x => x.LinesJson).HasColumnType("nvarchar(max)").IsRequired();
        version.Ignore(x => x.DisplayName);
        version.HasIndex(x => new { x.PackageId, x.Number }).IsUnique();
        version.HasOne<Package>().WithMany().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Restrict);
        version.HasOne<BomRevision>().WithMany().HasForeignKey(x => x.BomRevisionId).OnDelete(DeleteBehavior.Restrict);

        line.Property(x => x.MountingCode).HasMaxLength(50);
        line.Property(x => x.ComparisonDecision).HasMaxLength(20).IsUnicode(false);
        line.Property(x => x.DifferenceQuestion).HasMaxLength(2000);
        line.Property(x => x.CustomerAnswer).HasMaxLength(2000);

        Files<DocumentationFile>(model, DocumentationSchema);
        History<DocumentationChangeSet, DocumentationFieldChange>(model, DocumentationSchema, x => x.Changes);
    }

    // Obszar Usługi wspólne: Klienci, użytkownicy, role, grupy.
    private static void ConfigureShared(ModelBuilder model)
    {
        var customer = model.Entity<Customer>();
        customer.ToTable("Customers", SharedSchema);
        customer.HasKey(x => x.Id);
        customer.Property(x => x.Id).ValueGeneratedNever();
        customer.Property(x => x.Name).HasMaxLength(500).IsRequired();
        customer.Property(x => x.Code).HasMaxLength(500);
        customer.HasIndex(x => x.Name).IsUnique();

        // Kopie danych ProduMana na czas pilotażu (ADR-0014): te same nazwy i kolumny co widoki, tylko do odczytu.
        // Mapowane jako widoki — tabele tworzy migracja SQL-em, a po pilotażu zastąpią je prawdziwe widoki bez zmian w zapytaniach.
        var account = model.Entity<ProduManAccount>();
        account.ToView("users", "produman");
        account.HasKey(x => x.UserId);
        account.Ignore(x => x.Selectable);
        var department = model.Entity<Department>();
        department.ToView("Departments", "employees");
        department.HasKey(x => x.Id);

        // Uprawnienia konta jako JSON (ADR-0013, wzorzec ADR-0025 ProduMan); wersja chroni przed nadpisaniem cudzej zmiany.
        var access = model.Entity<UserAccess>();
        access.ToTable("UserAccess", SharedSchema, t => t.HasCheckConstraint("CK_UserAccess_Json", "ISJSON([PermissionsJson]) = 1"));
        access.HasKey(x => x.UserId);
        access.Property(x => x.PermissionsJson).HasColumnType("nvarchar(max)").IsRequired();
        access.Property(x => x.Version).IsConcurrencyToken();
        access.Property(x => x.ChangedBy).HasMaxLength(256).IsRequired();
        access.Ignore(x => x.Permissions);

        var group = model.Entity<Group>();
        group.ToTable("Groups", SharedSchema);
        group.HasKey(x => x.Id);
        group.Property(x => x.Id).ValueGeneratedNever();
        group.Property(x => x.Code).HasMaxLength(50).IsRequired();
        group.Property(x => x.Name).HasMaxLength(200).IsRequired();
        group.HasIndex(x => x.Code).IsUnique();
        group.HasData(
            new Group { Id = new("6f1c0b2e-0001-4d6a-9a51-6b1f0c2a0001"), Code = GroupCodes.ProjectService, Name = "Obsługa Projektu" },
            new Group { Id = new("6f1c0b2e-0002-4d6a-9a51-6b1f0c2a0002"), Code = GroupCodes.SmtTechnology, Name = "Technologia SMT" },
            new Group { Id = new("6f1c0b2e-0003-4d6a-9a51-6b1f0c2a0003"), Code = GroupCodes.Purchasing, Name = "Zakupy" },
            new Group { Id = new("6f1c0b2e-0004-4d6a-9a51-6b1f0c2a0004"), Code = GroupCodes.Warehouse, Name = "Magazyn" },
            new Group { Id = new("6f1c0b2e-0005-4d6a-9a51-6b1f0c2a0005"), Code = GroupCodes.Production, Name = "Produkcja" },
            new Group { Id = new("6f1c0b2e-0006-4d6a-9a51-6b1f0c2a0006"), Code = GroupCodes.CustomerAccountManagers, Name = "Opiekunowie klienta" });

        var member = model.Entity<GroupMember>();
        member.ToTable("GroupMembers", SharedSchema);
        member.HasKey(x => new { x.GroupId, x.UserId });
        member.HasOne<Group>().WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);

        History<SharedChangeSet, SharedFieldChange>(model, SharedSchema, x => x.Changes);
    }

    // Ta sama budowa historii w każdym schemacie (ADR-0007): ChangeSets + FieldChanges.
    private static void History<TSet, TChange>(ModelBuilder model, string schema, Expression<Func<TSet, IEnumerable<TChange>?>> changes)
        where TSet : ChangeSet where TChange : FieldChange
    {
        var set = model.Entity<TSet>();
        set.ToTable("ChangeSets", schema);
        set.HasKey(x => x.Id);
        set.Property(x => x.Id).ValueGeneratedNever();
        set.Property(x => x.Author).HasMaxLength(256).IsRequired();
        set.Property(x => x.SubjectType).HasMaxLength(100).IsRequired();
        set.Property(x => x.Operation).HasMaxLength(50).IsRequired();
        set.Property(x => x.Comment).HasMaxLength(4000);
        set.HasIndex(x => new { x.SubjectType, x.SubjectId, x.OccurredAtUtc });
        set.HasMany(changes).WithOne().HasForeignKey(x => x.ChangeSetId).OnDelete(DeleteBehavior.Restrict);

        var change = model.Entity<TChange>();
        change.ToTable("FieldChanges", schema);
        change.HasKey(x => x.Id);
        change.Property(x => x.ObjectType).HasMaxLength(100).IsRequired();
        change.Property(x => x.Field).HasMaxLength(512).IsRequired();
        change.Property(x => x.PreviousValue).HasColumnType("nvarchar(max)");
        change.Property(x => x.NewValue).HasColumnType("nvarchar(max)");
        change.HasIndex(x => new { x.ObjectType, x.ObjectId });
    }

    // Pliki źródłowe w schemacie obszaru, który je importuje (ADR-0008).
    private static void Files<T>(ModelBuilder model, string schema) where T : StoredFile
    {
        var file = model.Entity<T>();
        file.ToTable("Files", schema, t => t.HasCheckConstraint("CK_Files_Size", "[SizeBytes] >= 0 AND [SizeBytes] = DATALENGTH([Content])"));
        file.HasKey(x => x.Id);
        file.Property(x => x.Id).ValueGeneratedNever();
        file.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        file.Property(x => x.ContentType).HasMaxLength(200);
        file.Property(x => x.Sha256).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        file.Property(x => x.Content).HasColumnType("varbinary(max)").IsRequired();
        file.Property(x => x.UploadedBy).HasMaxLength(256).IsRequired();
        file.HasIndex(x => x.Sha256);
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
