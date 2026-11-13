using System.Data;
using System.Globalization;
using iPart.Core.Components;
using iPart.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit.Abstractions;

namespace iPart.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("IPART_RUN_SQL_TESTS") != "1")
            Skip = "Ustaw IPART_RUN_SQL_TESTS=1. Testy używają tylko lokalnych tabel #temp w tempdb; nie stosują migracji.";
    }
}

// Actual SQL Server tests. Only connection/history reads touch dataPart; all test data lives in #temp tables.
public sealed class SqlServerConstraintTests(ITestOutputHelper output)
{
    private static IPartDbContext ConfiguredContext()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "iPart.slnx"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Nie znaleziono katalogu repozytorium.");
        return new IPartDbContextFactory().CreateDbContext(["--settings", Path.Combine(directory.FullName, "src/iPart.App/appsettings.json")]);
    }

    [SqlServerFact]
    public async Task ExistingDatabaseIsReachableAndAppliedHistoryIsReadOnly()
    {
        await using var context = ConfiguredContext();
        Assert.True(await context.Database.CanConnectAsync());
        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        output.WriteLine("Migracje zastosowane do dataPart: " + (applied.Length == 0 ? "brak" : string.Join(", ", applied)));
        var known = context.Database.GetMigrations().ToArray();
        Assert.All(applied, migration => Assert.Contains(migration, known));
    }

    [SqlServerFact]
    public async Task UniqueBusinessKeyRejectsDuplicateNullCombinationsAndRespectsCase()
    {
        await using var context = ConfiguredContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Component))!;
        await using var connection = new SqlConnection(context.Database.GetConnectionString());
        await connection.OpenAsync();
        var check = entity.GetCheckConstraints().Single(x => x.Name == "CK_Components_MpnNormalized").Sql;
        await Execute(connection, $"CREATE TABLE #Keys (Mpn nvarchar(200) COLLATE Latin1_General_100_BIN2 NOT NULL, ManufacturerId int NULL, CustomerId int NULL, CHECK ({check})); CREATE UNIQUE INDEX UX_Keys ON #Keys(Mpn, ManufacturerId, CustomerId);");
        foreach (var (manufacturer, customer) in new (int?, int?)[] { (null, null), (1, null), (null, 1), (1, 1) })
        {
            await InsertKey(connection, "001X", manufacturer, customer);
            var error = await Assert.ThrowsAsync<SqlException>(() => InsertKey(connection, "001X", manufacturer, customer));
            Assert.Contains(error.Number, new[] { 2601, 2627 });
        }
        await InsertKey(connection, "001X", 1, 2);
        await InsertKey(connection, "001X", 2, 1);
        await InsertKey(connection, "001x", null, null);
        await InsertKey(connection, "001 X", null, null);
        await InsertKey(connection, "é", null, null);
        await InsertKey(connection, "e\u0301", null, null);
        await InsertKey(connection, "A", null, null);
        await InsertKey(connection, "A\0", null, null);
        await InsertKey(connection, "A\u0001", null, null);
        foreach (var invalid in new[] { "", " X", "X ", "\tX", "X\u00a0", "\u2000X" })
        {
            var error = await Assert.ThrowsAsync<SqlException>(() => InsertKey(connection, invalid, null, null));
            Assert.Equal(547, error.Number);
        }
    }

    [SqlServerFact]
    public async Task PadsChecksExecuteOnSqlServer()
    {
        await using var context = ConfiguredContext();
        await using var connection = new SqlConnection(context.Database.GetConnectionString());
        await connection.OpenAsync();
        await CreateTemporaryTable<ComponentPads>(context, connection, "#Pads");
        await Execute(connection, "INSERT #Pads DEFAULT VALUES; INSERT #Pads(RC, TT, DL, QL, BGA, TP, THT, PreTHT, PressFIT, MECH, BTC, NS) VALUES(0,2,3,4,3,0,2147483647,0,0,0,0,1);");
        foreach (var (field, invalid) in new[] { ("RC", 1), ("TT", 3), ("DL", 2), ("QL", 3), ("BGA", 2), ("TP", -1), ("THT", -1), ("PreTHT", -1), ("PressFIT", -1), ("MECH", -1) })
        {
            var error = await Assert.ThrowsAsync<SqlException>(() => Execute(connection, $"INSERT #Pads([{field}]) VALUES({invalid.ToString(CultureInfo.InvariantCulture)})"));
            Assert.Equal(547, error.Number);
        }
    }

    [SqlServerFact]
    public async Task CapacitorToleranceAndMagnitudeChecksExecuteOnSqlServer()
    {
        await using var context = ConfiguredContext();
        await using var connection = new SqlConnection(context.Database.GetConnectionString());
        await connection.OpenAsync();
        await CreateTemporaryTable<CapacitorParameters>(context, connection, "#Cap");
        await Execute(connection, "INSERT #Cap DEFAULT VALUES; INSERT #Cap(TolerancePercent) VALUES(0); INSERT #Cap(AbsoluteTolerancePf,EsrMilliOhms) VALUES(0,25);");
        foreach (var insert in new[] { "(TolerancePercent, AbsoluteTolerancePf) VALUES(0,0)", "(EsrMilliOhms) VALUES(-1)", "(LifetimeHours) VALUES(-1)", "(CapacitancePf) VALUES(-1)" })
        {
            var error = await Assert.ThrowsAsync<SqlException>(() => Execute(connection, "INSERT #Cap" + insert));
            Assert.Equal(547, error.Number);
        }
    }

    private static async Task CreateTemporaryTable<T>(IPartDbContext context, SqlConnection connection, string table)
    {
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(T))!;
        var columns = entity.GetProperties().Where(x => x.Name != "ComponentId")
            .Select(x => $"[{x.Name}] {x.GetColumnType()} NULL");
        var checks = entity.GetCheckConstraints().Select(x => $"CHECK ({x.Sql})");
        await Execute(connection, $"CREATE TABLE {table} ({string.Join(',', columns.Concat(checks))});");
    }

    private static async Task InsertKey(SqlConnection connection, string mpn, int? manufacturer, int? customer)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT #Keys(Mpn,ManufacturerId,CustomerId) VALUES(@mpn,@manufacturer,@customer)";
        command.Parameters.Add("@mpn", SqlDbType.NVarChar, 200).Value = mpn;
        command.Parameters.Add("@manufacturer", SqlDbType.Int).Value = manufacturer is null ? DBNull.Value : manufacturer.Value;
        command.Parameters.Add("@customer", SqlDbType.Int).Value = customer is null ? DBNull.Value : customer.Value;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task Execute(SqlConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
