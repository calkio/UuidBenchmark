namespace UuidBenchmark.Cli;

public class CliOptions
{
    /// <summary>all | v4 | v7builtin | v7uuidnext | v7medo | v7dbside</summary>
    public string Strategy { get; init; } = "all";
    public int TotalRows { get; init; } = 1_000_000;
    public int BatchSize { get; init; } = 1_000;
    public int ReportEveryRows { get; init; } = 100_000;
    public string? ConnectionString { get; init; }
    public bool SkipInsert { get; init; }

    public static CliOptions Parse(string[] args)
    {
        string strategy = "all";
        int totalRows = 1_000_000;
        int batchSize = 1_000;
        int reportEveryRows = 100_000;
        string? connectionString = null;
        bool skipInsert = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--strategy":
                    strategy = args[++i];
                    break;
                case "--total-rows":
                    totalRows = int.Parse(args[++i]);
                    break;
                case "--batch-size":
                    batchSize = int.Parse(args[++i]);
                    break;
                case "--report-every":
                    reportEveryRows = int.Parse(args[++i]);
                    break;
                case "--connection-string":
                    connectionString = args[++i];
                    break;
                case "--skip-insert":
                    // полезно, если хотите только посмотреть отчёт по уже
                    // загруженным ранее данным, не вставляя заново
                    skipInsert = true;
                    break;
                case "--help":
                case "-h":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        return new CliOptions
        {
            Strategy = strategy,
            TotalRows = totalRows,
            BatchSize = batchSize,
            ReportEveryRows = reportEveryRows,
            ConnectionString = connectionString,
            SkipInsert = skipInsert
        };
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            UuidBenchmark.Cli — сравнение стратегий генерации UUID при вставке в PostgreSQL

            Опции:
              --strategy <name>          all | v4 | v7builtin | v7uuidnext | v7medo | v7dbside  (по умолчанию: all)
              --total-rows <n>           сколько строк вставить на стратегию                    (по умолчанию: 1000000)
              --batch-size <n>           размер одного батча (SaveChanges)                      (по умолчанию: 1000)
              --report-every <n>         как часто фиксировать чекпоинт по времени               (по умолчанию: 100000)
              --connection-string <s>    строка подключения (иначе — переменная окружения
                                          UUID_BENCHMARK_CONNECTION_STRING или дефолт localhost)
              --skip-insert              не вставлять, только показать текущий отчёт по таблицам

            Пример:
              dotnet run --project src/UuidBenchmark.Cli -- --strategy all --total-rows 5000000 --batch-size 2000
            """);
    }
}
