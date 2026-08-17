using System.Diagnostics;
using UuidBenchmark.Database;

namespace UuidBenchmark.Cli;

public record InsertRunResult(int TotalRows, TimeSpan TotalElapsed, List<(int Rows, TimeSpan Elapsed)> Checkpoints);

public static class BatchInsertRunner
{
    /// <summary>
    /// Вставляет totalRows строк батчами по batchSize через AddRange + SaveChanges —
    /// намеренно тот же путь, каким ваше приложение реально пишет в БД через EF Core,
    /// а не "быстрый" COPY в обход ORM.
    /// </summary>
    public static async Task<InsertRunResult> RunAsync<TEntity>(
        Func<BenchmarkDbContext> contextFactory,
        int totalRows,
        int batchSize,
        int reportEveryRows,
        Func<int, TEntity> rowFactory)
        where TEntity : BenchmarkRowBase
    {
        var sw = Stopwatch.StartNew();
        var checkpoints = new List<(int, TimeSpan)>();
        var inserted = 0;
        var lastReportedAt = 0;

        while (inserted < totalRows)
        {
            var currentBatchSize = Math.Min(batchSize, totalRows - inserted);

            await using var ctx = contextFactory();
            // Отключаем автообнаружение изменений — при batch-insert нам не нужно,
            // чтобы EF Core на каждый Add сканировал весь граф на предмет правок.
            ctx.ChangeTracker.AutoDetectChangesEnabled = false;

            var batch = new List<TEntity>(currentBatchSize);
            for (var i = 0; i < currentBatchSize; i++)
                batch.Add(rowFactory(inserted + i));

            ctx.Set<TEntity>().AddRange(batch);
            await ctx.SaveChangesAsync();

            inserted += currentBatchSize;

            if (inserted - lastReportedAt >= reportEveryRows || inserted == totalRows)
            {
                checkpoints.Add((inserted, sw.Elapsed));
                lastReportedAt = inserted;
                Console.WriteLine($"  ... {inserted,10:N0} / {totalRows:N0} строк, {sw.Elapsed.TotalSeconds:F1}s");
            }
        }

        sw.Stop();
        return new InsertRunResult(totalRows, sw.Elapsed, checkpoints);
    }
}
