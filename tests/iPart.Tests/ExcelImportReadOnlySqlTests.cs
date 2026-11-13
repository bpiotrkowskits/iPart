using System.Security.Cryptography;
using System.Text;
using iPart.Infrastructure;
using iPart.Infrastructure.Import;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace iPart.Tests;

public sealed class ImportReadOnlySqlFactAttribute : FactAttribute
{
    public ImportReadOnlySqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("IPART_RUN_IMPORT_SQL_TESTS") != "1")
            Skip = "Ustaw IPART_RUN_IMPORT_SQL_TESTS=1. Test wyłącznie odczytuje lokalną dataPart i sprawdza niezmienność bazy i pliku.";
    }
}

public sealed class ExcelImportReadOnlySqlTests
{
    [ImportReadOnlySqlFact]
    public async Task PreviewIssuesOnlySelectAndLeavesAllDatabaseRowsAndInputBytesUnchanged()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "iPart.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        await using var configured = new IPartDbContextFactory().CreateDbContext(["--settings", Path.Combine(root.FullName, "src/iPart.App/appsettings.json")]);
        var settings = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
        Assert.Equal(@"(localdb)\MSSQLLocalDB", settings.DataSource);
        Assert.Equal("dataPart", settings.InitialCatalog);
        await using var connection = new SqlConnection(settings.ConnectionString);
        await connection.OpenAsync();
        var before = await Snapshot(connection);
        using var workbook = new ExcelImportTests.WorkbookFile(new ExcelImportTests.SheetSpec("partsData",
            [ExcelImportTests.Row("Part Number", "Mounting", "RC", "BTC"), ExcelImportTests.Row("IMPORT-READONLY-" + Guid.NewGuid().ToString("N"), "SMT", 0, false)]));
        var inputHash = SHA256.HashData(await File.ReadAllBytesAsync(workbook.Path));
        var guard = new SelectOnly();
        var options = new DbContextOptionsBuilder<IPartDbContext>().UseSqlServer(settings.ConnectionString).AddInterceptors(guard).Options;
        var service = new ExcelPreviewService(new SqlImportReferenceReader(new IPartContextFactory(options)));
        var result = await service.AnalyzeAsync(workbook.Path, new());
        Assert.Single(result.Records);
        Assert.Equal(1, result.ReadyRecords);
        Assert.Equal(3, guard.Selects);
        Assert.Equal(before, await Snapshot(connection));
        Assert.Equal(inputHash, SHA256.HashData(await File.ReadAllBytesAsync(workbook.Path)));
    }

    private sealed class SelectOnly : DbCommandInterceptor
    {
        public int Selects { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Assert.StartsWith("SELECT", command.CommandText.TrimStart()); Selects++; return ValueTask.FromResult(result);
        }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Import preview must not execute mutations.");
    }

    private static async Task<string> Snapshot(SqlConnection connection)
    {
        var tables = new List<(string Schema, string Name)>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT SCHEMA_NAME(schema_id),name FROM sys.tables WHERE is_ms_shipped=0 ORDER BY 1,2";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) tables.Add((reader.GetString(0), reader.GetString(1)));
        }
        var data = new StringBuilder();
        foreach (var (schema, name) in tables)
        {
            data.Append(schema).Append('.').Append(name);
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM [{schema.Replace("]", "]]")}].[{name.Replace("]", "]]")}] FOR JSON PATH, INCLUDE_NULL_VALUES";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) data.Append(reader.GetString(0));
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data.ToString())));
    }
}
