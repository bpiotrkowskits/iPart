using System.Data.Common;
using iPart.Core;
using iPart.Core.Components;
using iPart.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace iPart.Tests;

public sealed class SqlServerDatabaseFactAttribute : FactAttribute
{
    public SqlServerDatabaseFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("IPART_RUN_DATABASE_TESTS") != "1")
            Skip = "Ustaw IPART_RUN_DATABASE_TESTS=1 dla testów tabel dataPart; wymagają zastosowanej migracji i zawsze wykonują ROLLBACK.";
    }
}

// Each test owns an outer SQL transaction. Contexts and the real service enlist in it;
// disposing the scope rolls it back and verifies that ALL table row counts are unchanged.
public sealed class SqlServerDatabaseTests
{
    [SqlServerDatabaseFact]
    public async Task SchemaHistoryAndApplicationConnectionAreConsistent()
    {
        await using var database = await RollbackDatabase.OpenAsync();
        await using var context = database.CreateDbContext();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync());
        Assert.Equal(DatabaseConnectionStatus.Connected,
            await new SqlServerConnectionChecker(database.Connection.ConnectionString).CheckAsync());
        // 19 tabel modelu komponentów + 5 tabel pakietów/BOM + __EFMigrationsHistory.
        Assert.Equal(25L, await database.Scalar("SELECT COUNT_BIG(*) FROM sys.tables WHERE is_ms_shipped=0"));
        Assert.Equal(0L, await database.Scalar("SELECT COUNT_BIG(*) FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1"));
        Assert.Equal(0L, await database.Scalar("SELECT COUNT_BIG(*) FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1"));
        Assert.Equal(1L, await database.Scalar("SELECT COUNT_BIG(*) FROM sys.indexes WHERE name=N'UX_Components_BusinessKey' AND is_unique=1 AND has_filter=0"));
    }

    [SqlServerDatabaseFact]
    public async Task BusinessKeyIsEnforcedInActualComponentsTableIncludingEveryNullCombination()
    {
        await using var database = await RollbackDatabase.OpenAsync();
        var manufacturer = await database.NewDictionary("Manufacturers");
        var customer = await database.NewDictionary("Customers");
        foreach (var (m, c) in new (int?, int?)[] { (null, null), (manufacturer, null), (null, customer), (manufacturer, customer) })
        {
            var mpn = "TEST-" + Guid.NewGuid().ToString("N");
            await database.InsertComponent(mpn, m, c);
            var exception = await Assert.ThrowsAsync<SqlException>(() => database.InsertComponent(mpn, m, c));
            Assert.Contains(exception.Number, new[] { 2601, 2627 });
            var result = await database.Service().SaveAsync(new() { Mpn = mpn, ManufacturerId = m, CustomerId = c }, null);
            Assert.False(result.Success);
            Assert.Equal(SaveFailure.DuplicateBusinessKey, result.Failure);
            Assert.Contains("MPN", result.Message);
        }
    }

    [SqlServerDatabaseFact]
    public async Task DifferentCustomersManufacturersAndMpnCaseAreAllowed()
    {
        await using var database = await RollbackDatabase.OpenAsync();
        var m1 = await database.NewDictionary("Manufacturers");
        var m2 = await database.NewDictionary("Manufacturers");
        var c1 = await database.NewDictionary("Customers");
        var c2 = await database.NewDictionary("Customers");
        foreach (var (mpn, m, c) in new (string, int?, int?)[] {
            ("001-AbC", m1, c1), ("001-AbC", m2, c1), ("001-AbC", m1, c2),
            ("001-abc", m1, c1), ("001-AbC", null, c1), ("001-AbC", m1, null), ("001-AbC", null, null) })
        {
            var result = await database.Service().SaveAsync(new() { Mpn = mpn, ManufacturerId = m, CustomerId = c }, null);
            Assert.True(result.Success, result.Message);
        }
    }

    [SqlServerDatabaseFact]
    public async Task PadsPreserveNullZeroAndFalseAndSqlRejectsInvalidCounts()
    {
        await using var database = await RollbackDatabase.OpenAsync();
        var service = database.Service();
        var component = new Component { Mpn = "TEST-" + Guid.NewGuid().ToString("N"),
            Pads = new() { RC = null, TT = 0, DL = 3, QL = 4, BGA = 3, TP = 0, THT = int.MaxValue, BTC = false, NS = null } };
        var result = await service.SaveAsync(component, null);
        Assert.True(result.Success, result.Message);
        var read = (await service.GetAsync(component.Id))!;
        Assert.Null(read.Pads!.RC);
        Assert.Equal(0, read.Pads.TT);
        Assert.Equal(0, read.Pads.TP);
        Assert.Equal(int.MaxValue, read.Pads.THT);
        Assert.False(read.Pads.BTC);
        Assert.Null(read.Pads.NS);
        foreach (var (field, value) in new[] { ("RC", 1), ("TT", 1), ("DL", 2), ("QL", 3), ("BGA", 2), ("TP", -1), ("THT", -1), ("PreTHT", -1), ("PressFIT", -1), ("MECH", -1) })
        {
            var error = await Assert.ThrowsAsync<SqlException>(() => database.Execute($"UPDATE [dataPart].[ComponentPads] SET [{field}]=@value WHERE [ComponentId]=@id", ("@value", value), ("@id", component.Id)));
            Assert.Equal(547, error.Number);
        }
        read.Pads.BTC = null;
        read.Pads.NS = false;
        read.Pads.RC = 0;
        Assert.True((await service.SaveAsync(read, result.Version)).Success);
        read = (await service.GetAsync(component.Id))!;
        Assert.Null(read.Pads!.BTC);
        Assert.False(read.Pads.NS);
        Assert.Equal(0, read.Pads.RC);
    }

    [SqlServerDatabaseFact]
    public async Task CapacitanceAndEsrRoundTripExactlyAndBothTolerancesAreRejectedBySql()
    {
        await using var database = await RollbackDatabase.OpenAsync();
        var service = database.Service();
        foreach (var pf in new[] { 100000.123456789m, 9999999999999999999.999999999m, .000000001m })
        {
            var component = new Component { Mpn = "TEST-" + Guid.NewGuid().ToString("N"),
                Capacitor = new() { CapacitancePf = pf, EsrMilliOhms = 25.123456789m, LowEsr = false } };
            Assert.True((await service.SaveAsync(component, null)).Success);
            var read = (await service.GetAsync(component.Id))!;
            Assert.Equal(pf, read.Capacitor!.CapacitancePf);
            Assert.Equal(25.123456789m, read.Capacitor.EsrMilliOhms);
            Assert.False(read.Capacitor.LowEsr);
            var exception = await Assert.ThrowsAsync<SqlException>(() => database.Execute(
                "UPDATE CapacitorParameters SET TolerancePercent=0,AbsoluteTolerancePf=0 WHERE ComponentId=@id", ("@id", component.Id)));
            Assert.Equal(547, exception.Number);
        }
        var invalid = new Component { Mpn = "TEST-" + Guid.NewGuid().ToString("N"), Capacitor = new() { CapacitancePf = .0000000001m } };
        Assert.Equal(SaveFailure.Validation, (await service.SaveAsync(invalid, null)).Failure);
        Assert.Null(await service.GetAsync(invalid.Id));
    }

    [SqlServerDatabaseFact]
    public async Task AggregateAndHistoryAreSavedTogetherAndNoOpDoesNotCreateHistory()
    {
        await using var database = await RollbackDatabase.OpenAsync();
        var service = database.Service();
        var component = new Component { Mpn = "  001-" + Guid.NewGuid().ToString("N") + "  ",
            Resistor = new() { ResistanceOhms = 4700.123456789012m, PowerWatts = .25m },
            Pads = new() { TP = 0, BTC = false }, Qualifications = [new() { QualificationId = 1 }],
            Links = [new() { Kind = ComponentLinkKind.Documentation, Url = "https://example.org/test" }] };
        var saved = await service.SaveAsync(component, null);
        Assert.True(saved.Success, saved.Message);
        await using var context = database.CreateDbContext();
        var history = await context.ComponentHistory.Where(x => x.ComponentId == component.Id).ToListAsync();
        Assert.NotEmpty(history);
        Assert.Single(history.Select(x => x.OperationId).Distinct());
        Assert.Single(history.Select(x => x.OccurredAtUtc).Distinct());
        Assert.All(history, x => { Assert.Equal("iPart integration test", x.Author); Assert.Equal(TimeSpan.Zero, x.OccurredAtUtc.Offset); Assert.Equal(HistoryOperation.Created, x.Operation); });
        Assert.Contains(history, x => x.FieldPath == "Resistor.ResistanceOhms");
        Assert.Contains(history, x => x.FieldPath == "Pads.TP" && x.PreviousValue is null && x.NewValue == "0");
        Assert.Contains(history, x => x.FieldPath == "Pads.BTC" && x.NewValue == "false");
        Assert.Contains(history, x => x.FieldPath == "Qualifications.1");
        Assert.Contains(history, x => x.FieldPath.EndsWith(".Url"));
        var read = (await service.GetAsync(component.Id))!;
        Assert.Equal(component.Resistor.ResistanceOhms, read.Resistor!.ResistanceOhms);
        Assert.Single(read.Links);
        Assert.Single(read.Qualifications);
        var noOp = await service.SaveAsync(read, saved.Version);
        Assert.True(noOp.Success);
        Assert.Equal(saved.Version, noOp.Version);
        Assert.Equal(history.Count, await context.ComponentHistory.CountAsync(x => x.ComponentId == component.Id));
        read.Pads!.TP = null;
        read.Pads.BTC = null;
        read.Resistor.PowerWatts = 1;
        read.Links.Clear();
        read.Qualifications.Clear();
        var updated = await service.SaveAsync(read, noOp.Version);
        Assert.True(updated.Success, updated.Message);
        var updates = await context.ComponentHistory.Where(x => x.ComponentId == component.Id && x.Operation == HistoryOperation.Updated).ToListAsync();
        Assert.Single(updates.Select(x => x.OperationId).Distinct());
        Assert.Contains(updates, x => x.FieldPath == "Pads.TP" && x.PreviousValue == "0" && x.NewValue is null);
        Assert.Contains(updates, x => x.FieldPath == "Pads.BTC" && x.PreviousValue == "false" && x.NewValue is null);
        read = (await service.GetAsync(component.Id))!;
        Assert.Empty(read.Links);
        Assert.Empty(read.Qualifications);
    }

    [SqlServerDatabaseFact]
    public async Task FailureWhileWritingHistoryRollsBackAlreadyInsertedComponent()
    {
        await using var database = await RollbackDatabase.OpenAsync();
        var component = new Component { Mpn = "TEST-" + Guid.NewGuid().ToString("N"), Resistor = new() { ResistanceOhms = 100 } };
        var fault = new FailHistoryInsert(component.Id);
        var service = database.Service(interceptors: [fault]);
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => service.SaveAsync(component, null));
        Assert.Contains("Injected history write failure", exception.ToString());
        Assert.True(fault.SawInsertedComponent, "Failure must occur AFTER the component INSERT, not before any write.");
        Assert.Null(await database.Service().GetAsync(component.Id));
        Assert.Equal(0L, await database.Scalar("SELECT COUNT_BIG(*) FROM dataPart.ComponentHistory WHERE ComponentId=@id", ("@id", component.Id)));
        Assert.Equal(0L, await database.Scalar("SELECT COUNT_BIG(*) FROM ResistorParameters WHERE ComponentId=@id", ("@id", component.Id)));
    }

    [SqlServerDatabaseFact]
    public async Task OwnedServiceTransactionRollsBackWhenHistoryInsertFails()
    {
        await using var database = await RollbackDatabase.OpenAsync();
        var component = new Component { Mpn = "TEST-" + Guid.NewGuid().ToString("N"), Resistor = new() { ResistanceOhms = 100 } };
        var fault = new FailHistoryInsert(component.Id);
        var options = new DbContextOptionsBuilder<IPartDbContext>()
            .UseSqlServer(database.Connection.ConnectionString, sql => sql.MaxBatchSize(1)).AddInterceptors(fault).Options;
        var service = new SqlServerComponentService(new IPartContextFactory(options), new CallbackUser());
        await Assert.ThrowsAsync<DbUpdateException>(() => service.SaveAsync(component, null));
        Assert.True(fault.SawInsertedComponent);
        Assert.Null(await database.Service().GetAsync(component.Id));
        Assert.Equal(0L, await database.Scalar("SELECT COUNT_BIG(*) FROM dataPart.ComponentHistory WHERE ComponentId=@id", ("@id", component.Id)));
    }

    [SqlServerDatabaseFact]
    public async Task OwnedServiceTransactionContainsCompleteAggregateBeforeCommitAndTestRollsItBack()
    {
        await using var database = await RollbackDatabase.OpenAsync();
        var component = new Component { Mpn = "TEST-" + Guid.NewGuid().ToString("N"), Pads = new() { TP = 0 } };
        var rollback = new ObserveAndRollbackCommit(component.Id);
        var options = new DbContextOptionsBuilder<IPartDbContext>()
            .UseSqlServer(database.Connection.ConnectionString).AddInterceptors(rollback).Options;
        var service = new SqlServerComponentService(new IPartContextFactory(options), new CallbackUser());
        var result = await service.SaveAsync(component, null);
        Assert.True(result.Success, result.Message);
        Assert.True(rollback.SawCompleteAggregate);
        Assert.Null(await database.Service().GetAsync(component.Id));
        Assert.Equal(0L, await database.Scalar("SELECT COUNT_BIG(*) FROM dataPart.ComponentHistory WHERE ComponentId=@id", ("@id", component.Id)));
    }

    [SqlServerDatabaseFact]
    public async Task TwoEditorsCannotOverwriteCommonFieldsOrParameterOnlyChanges()
    {
        await using var database = await RollbackDatabase.OpenAsync();
        var service = database.Service();
        foreach (var parametersOnly in new[] { false, true })
        {
            var component = new Component { Mpn = "TEST-" + Guid.NewGuid().ToString("N"), Resistor = new() { PowerWatts = .25m } };
            Assert.True((await service.SaveAsync(component, null)).Success);
            var first = (await service.GetAsync(component.Id))!;
            var second = (await service.GetAsync(component.Id))!;
            if (parametersOnly) { first.Resistor!.PowerWatts = 1; second.Resistor!.PowerWatts = 2; }
            else { first.NamePl = "first"; second.NamePl = "second"; }
            var saved = await service.SaveAsync(first, first.Version);
            Assert.True(saved.Success, saved.Message);
            Assert.NotEqual(second.Version, saved.Version);
            var historyBefore = await database.Scalar("SELECT COUNT_BIG(*) FROM dataPart.ComponentHistory WHERE ComponentId=@id", ("@id", component.Id));
            var conflict = await service.SaveAsync(second, second.Version);
            Assert.Equal(SaveFailure.ConcurrentEdit, conflict.Failure);
            Assert.False(conflict.Success);
            Assert.Contains("innego użytkownika", conflict.Message);
            var read = (await service.GetAsync(component.Id))!;
            if (parametersOnly) Assert.Equal(1, read.Resistor!.PowerWatts);
            else Assert.Equal("first", read.NamePl);
            Assert.Equal(historyBefore, await database.Scalar("SELECT COUNT_BIG(*) FROM dataPart.ComponentHistory WHERE ComponentId=@id", ("@id", component.Id)));
        }
    }

    [SqlServerDatabaseFact]
    public async Task VersionRaceAfterLoadIsRejectedBySqlUpdateAndRollsBackParametersAndHistory()
    {
        await using var database = await RollbackDatabase.OpenAsync();
        var normalService = database.Service();
        var component = new Component { Mpn = "TEST-" + Guid.NewGuid().ToString("N"), Resistor = new() { PowerWatts = .25m } };
        Assert.True((await normalService.SaveAsync(component, null)).Success);
        var edit = (await normalService.GetAsync(component.Id))!;
        edit.Resistor!.PowerWatts = 2;
        var competingVersion = Guid.NewGuid();
        var historyCount = await database.Scalar("SELECT COUNT_BIG(*) FROM dataPart.ComponentHistory WHERE ComponentId=@id", ("@id", component.Id));
        // Deterministically interleave a competing UPDATE after service load/precheck, before its SaveChanges.
        var race = new CallbackUser(() => database.ExecuteSync("UPDATE dataPart.Components SET Version=@version WHERE Id=@id", ("@version", competingVersion), ("@id", edit.Id)));
        var result = await database.Service(race).SaveAsync(edit, edit.Version);
        Assert.Equal(SaveFailure.ConcurrentEdit, result.Failure);
        var read = (await normalService.GetAsync(edit.Id))!;
        Assert.Equal(competingVersion, read.Version);
        Assert.Equal(.25m, read.Resistor!.PowerWatts);
        Assert.Equal(historyCount, await database.Scalar("SELECT COUNT_BIG(*) FROM dataPart.ComponentHistory WHERE ComponentId=@id", ("@id", edit.Id)));
    }

    [SqlServerDatabaseFact]
    public async Task SubcategoryMismatchIsRejectedByTheActualCompositeForeignKey()
    {
        await using var database = await RollbackDatabase.OpenAsync();
        var category1 = await database.NewDictionary("Categories");
        var category2 = await database.NewDictionary("Categories");
        var subcategory = (int)await database.Scalar("INSERT Subcategories(Name,CategoryId) OUTPUT INSERTED.Id VALUES(N'Test subcategory',@category)", ("@category", category1));
        var id = await database.InsertComponent("TEST-" + Guid.NewGuid().ToString("N"));
        var exception = await Assert.ThrowsAsync<SqlException>(() => database.Execute("UPDATE dataPart.Components SET CategoryId=@category,SubcategoryId=@subcategory WHERE Id=@id", ("@category", category2), ("@subcategory", subcategory), ("@id", id)));
        Assert.Equal(547, exception.Number);
        await database.Execute("UPDATE dataPart.Components SET CategoryId=@category,SubcategoryId=@subcategory WHERE Id=@id", ("@category", category1), ("@subcategory", subcategory), ("@id", id));
    }

    private sealed class CallbackUser(Action? beforeAuthor = null) : ICurrentUser
    {
        public string GetAuthor() { beforeAuthor?.Invoke(); return "iPart integration test"; }
    }

    private sealed class FailHistoryInsert(Guid componentId) : DbCommandInterceptor
    {
        public bool SawInsertedComponent { get; private set; }
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO [dataPart].[ComponentHistory]", StringComparison.Ordinal))
            {
                using var probe = command.Connection!.CreateCommand();
                probe.Transaction = command.Transaction;
                probe.CommandText = "SELECT COUNT_BIG(*) FROM dataPart.Components WHERE Id=@id";
                var parameter = probe.CreateParameter(); parameter.ParameterName = "@id"; parameter.Value = componentId; probe.Parameters.Add(parameter);
                SawInsertedComponent = Convert.ToInt64(await probe.ExecuteScalarAsync(cancellationToken)) == 1;
                throw new InvalidOperationException("Injected history write failure");
            }
            return result;
        }
    }

    private sealed class ObserveAndRollbackCommit(Guid componentId) : DbTransactionInterceptor
    {
        public bool SawCompleteAggregate { get; private set; }
        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            using var probe = transaction.Connection!.CreateCommand();
            probe.Transaction = transaction;
            probe.CommandText = "SELECT (SELECT COUNT_BIG(*) FROM dataPart.Components WHERE Id=@id), (SELECT COUNT_BIG(*) FROM dataPart.ComponentPads WHERE ComponentId=@id), (SELECT COUNT_BIG(*) FROM dataPart.ComponentHistory WHERE ComponentId=@id)";
            var parameter = probe.CreateParameter(); parameter.ParameterName = "@id"; parameter.Value = componentId; probe.Parameters.Add(parameter);
            await using var reader = await probe.ExecuteReaderAsync(cancellationToken);
            Assert.True(await reader.ReadAsync(cancellationToken));
            SawCompleteAggregate = reader.GetInt64(0) == 1 && reader.GetInt64(1) == 1 && reader.GetInt64(2) > 0;
            // Exercise the owned transaction's commit path, but prevent committing test data.
            // Disposing the still-uncommitted underlying SqlTransaction performs rollback.
            return InterceptionResult.Suppress();
        }
    }

    private sealed class RollbackDatabase : IAsyncDisposable, IDbContextFactory<IPartDbContext>
    {
        public SqlConnection Connection { get; }
        private readonly SqlTransaction _transaction;
        private readonly Dictionary<string, long> _before;

        private RollbackDatabase(SqlConnection connection, SqlTransaction transaction, Dictionary<string, long> before)
        { Connection = connection; _transaction = transaction; _before = before; }

        public static async Task<RollbackDatabase> OpenAsync()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "iPart.slnx"))) directory = directory.Parent;
            if (directory is null) throw new InvalidOperationException("Repository not found.");
            await using var configured = new IPartDbContextFactory().CreateDbContext(["--settings", Path.Combine(directory.FullName, "src/iPart.App/appsettings.json")]);
            var settings = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
            if (settings.DataSource != @"(localdb)\MSSQLLocalDB" || settings.InitialCatalog != "dataPart" || settings.MultipleActiveResultSets)
                throw new InvalidOperationException("Tests require local dataPart and SQL savepoints (MARS disabled).");
            Assert.Empty(await configured.Database.GetPendingMigrationsAsync());
            var connection = new SqlConnection(settings.ConnectionString);
            await connection.OpenAsync();
            var before = await RowCounts(connection);
            var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            return new(connection, transaction, before);
        }

        public IPartDbContext CreateDbContext() => CreateContext([]);
        private IPartDbContext CreateContext(IInterceptor[] interceptors)
        {
            var options = new DbContextOptionsBuilder<IPartDbContext>().UseSqlServer(Connection, sql => sql.MaxBatchSize(1))
                .AddInterceptors(interceptors).Options;
            var context = new IPartDbContext(options);
            context.Database.UseTransaction(_transaction);
            return context;
        }

        public SqlServerComponentService Service(ICurrentUser? user = null, IInterceptor[]? interceptors = null) =>
            new(new TestContextFactory(() => CreateContext(interceptors ?? [])), user ?? new CallbackUser());

        public async Task<int> NewDictionary(string table)
        {
            if (table is not "Manufacturers" and not "Customers" and not "Categories") throw new ArgumentException(nameof(table));
            return (int)await Scalar($"INSERT [{table}] (Name) OUTPUT INSERTED.Id VALUES (@name)", ("@name", "integration-" + Guid.NewGuid().ToString("N")));
        }

        public async Task<Guid> InsertComponent(string mpn, int? manufacturer = null, int? customer = null)
        {
            var id = Guid.NewGuid();
            await Execute("INSERT Components(Id,Mpn,Version,ManufacturerId,CustomerId) VALUES(@id,@mpn,@version,@manufacturer,@customer)",
                ("@id", id), ("@mpn", mpn), ("@version", Guid.NewGuid()), ("@manufacturer", manufacturer), ("@customer", customer));
            return id;
        }

        private SqlCommand Command(string sql, (string Name, object? Value)[] parameters)
        {
            var command = Connection.CreateCommand(); command.Transaction = _transaction; command.CommandText = sql;
            foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            return command;
        }
        public async Task<long> Scalar(string sql, params (string Name, object? Value)[] parameters)
        { using var command = Command(sql, parameters); return Convert.ToInt64(await command.ExecuteScalarAsync()); }
        public async Task Execute(string sql, params (string Name, object? Value)[] parameters)
        { using var command = Command(sql, parameters); await command.ExecuteNonQueryAsync(); }
        public void ExecuteSync(string sql, params (string Name, object? Value)[] parameters)
        { using var command = Command(sql, parameters); command.ExecuteNonQuery(); }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                var after = await RowCounts(Connection);
                Assert.Equal(_before.OrderBy(x => x.Key), after.OrderBy(x => x.Key));
            }
            finally { await Connection.DisposeAsync(); }
        }

        private static async Task<Dictionary<string, long>> RowCounts(SqlConnection connection)
        {
            var tables = new List<(string Schema, string Table)>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT SCHEMA_NAME(schema_id), name FROM sys.tables WHERE is_ms_shipped=0";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) tables.Add((reader.GetString(0), reader.GetString(1)));
            }
            var result = new Dictionary<string, long>();
            foreach (var (schema, table) in tables)
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT COUNT_BIG(*) FROM [{schema.Replace("]", "]]")}].[{table.Replace("]", "]]")}]";
                result[schema + "." + table] = Convert.ToInt64(await command.ExecuteScalarAsync());
            }
            return result;
        }

        private sealed class TestContextFactory(Func<IPartDbContext> create) : IDbContextFactory<IPartDbContext>
        { public IPartDbContext CreateDbContext() => create(); }
    }
}
