using Microsoft.EntityFrameworkCore;
using Npgsql;
using UuidBenchmark.Cli;
using UuidBenchmark.Database;

var options = CliOptions.Parse(args);

var connectionString = options.ConnectionString
    ?? Environment.GetEnvironmentVariable("UUID_BENCHMARK_CONNECTION_STRING")
    ?? "Host=localhost;Port=5432;Database=uuid_benchmark;Username=postgres;Password=postgres";

BenchmarkDbContext ContextFactory()
{
    var builder = new DbContextOptionsBuilder<BenchmarkDbContext>().UseNpgsql(connectionString);
    return new BenchmarkDbContext(builder.Options);
}

await using (var ctx = ContextFactory())
{
    await ctx.Database.EnsureCreatedAsync();
}

var allStrategies = new[] { "v4", "v7builtin", "v7uuidnext", "v7medo", "v7dbside" };
var strategiesToRun = options.Strategy == "all" ? allStrategies : new[] { options.Strategy };

if (!options.SkipInsert)
{
    var payload = new string('x', 200); // имитация реалистичной ширины строки

    foreach (var strategy in strategiesToRun)
    {
        Console.WriteLine($"=== {strategy}: вставляем {options.TotalRows:N0} строк батчами по {options.BatchSize:N0} ===");

        InsertRunResult result = strategy switch
        {
            "v4" => await BatchInsertRunner.RunAsync<RowV4>(ContextFactory, options.TotalRows, options.BatchSize, options.ReportEveryRows,
                i => new RowV4 { Payload = payload, SequenceNo = i }),
            "v7builtin" => await BatchInsertRunner.RunAsync<RowV7BuiltIn>(ContextFactory, options.TotalRows, options.BatchSize, options.ReportEveryRows,
                i => new RowV7BuiltIn { Payload = payload, SequenceNo = i }),
            "v7uuidnext" => await BatchInsertRunner.RunAsync<RowV7UuidNext>(ContextFactory, options.TotalRows, options.BatchSize, options.ReportEveryRows,
                i => new RowV7UuidNext { Payload = payload, SequenceNo = i }),
            "v7medo" => await BatchInsertRunner.RunAsync<RowV7Medo>(ContextFactory, options.TotalRows, options.BatchSize, options.ReportEveryRows,
                i => new RowV7Medo { Payload = payload, SequenceNo = i }),
            "v7dbside" => await BatchInsertRunner.RunAsync<RowV7DbSide>(ContextFactory, options.TotalRows, options.BatchSize, options.ReportEveryRows,
                i => new RowV7DbSide { Payload = payload, SequenceNo = i }),
            _ => throw new ArgumentException($"Неизвестная стратегия: {strategy}")
        };

        var rowsPerSec = result.TotalRows / result.TotalElapsed.TotalSeconds;
        Console.WriteLine($"Итого: {result.TotalRows:N0} строк за {result.TotalElapsed.TotalSeconds:F2}s ({rowsPerSec:F0} строк/сек)");
        Console.WriteLine();
    }
}

Console.WriteLine("=== Отчёт по фрагментации таблиц/индексов (pgstattuple / pgstatindex) ===");
Console.WriteLine();

var tableNames = new[] { "rows_v4", "rows_v7_builtin", "rows_v7_uuidnext", "rows_v7_medo", "rows_v7_dbside" };

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

Console.WriteLine($"{"table",-20} {"rows",10} {"table size",12} {"index size",12} {"leaf density",13} {"fragmentation",14} {"dead tuple %",13}");

foreach (var table in tableNames)
{
    var stats = await FragmentationReport.GetAsync(connection, table);
    Console.WriteLine(
        $"{stats.TableName,-20} {stats.TupleCount,10:N0} {FormatBytes(stats.TableSizeBytes),12} " +
        $"{FormatBytes(stats.IndexSizeBytes),12} {stats.AvgLeafDensity,12:F1}% {stats.LeafFragmentation,13:F1}% " +
        $"{stats.DeadTuplePercent,12:F1}%");
}

Console.WriteLine();
Console.WriteLine("Подсказка: сравнивайте avg_leaf_density (выше = плотнее уложен индекс, меньше");
Console.WriteLine("'воздуха' от page split'ов) и leaf_fragmentation (ниже = страницы лежат на диске");
Console.WriteLine("более последовательно, меньше random I/O при обходе индекса).");

static string FormatBytes(long bytes) => bytes switch
{
    >= 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F2} GB",
    >= 1024 * 1024 => $"{bytes / (1024.0 * 1024):F2} MB",
    >= 1024 => $"{bytes / 1024.0:F2} KB",
    _ => $"{bytes} B"
};
