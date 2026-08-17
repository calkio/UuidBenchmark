using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using Medo;
using UUIDNext;

namespace UuidBenchmark.Generation;

/// <summary>
/// По умолчанию BenchmarkDotNet запускает каждый бенчмарк в отдельном
/// новом процессе — это точнее, но на Windows часто упирается в то, что
/// антивирус (в частности Windows Defender) блокирует запуск свежесобранных
/// exe. In-process toolchain выполняет всё в текущем процессе — без
/// спавна новых exe, а значит без этой проблемы.
/// </summary>
public class InProcessConfig : ManualConfig
{
    public InProcessConfig()
    {
        AddJob(Job.Default.WithToolchain(InProcessNoEmitToolchain.Instance));
    }
}

/// <summary>
/// Чистая скорость генерации одного идентификатора — без похода в БД.
/// Показывает CPU/allocation overhead каждого варианта, актуально при
/// высокой частоте вставок (тысячи insert/сек и выше).
/// </summary>
[MemoryDiagnoser]
[Config(typeof(InProcessConfig))]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class GenerationBenchmarks
{
    [Benchmark(Baseline = true, Description = "Guid.NewGuid() (v4)")]
    public Guid V4() => Guid.NewGuid();

    [Benchmark(Description = "Guid.CreateVersion7() (встроенный, .NET 9+)")]
    public Guid V7BuiltIn() => Guid.CreateVersion7();

    [Benchmark(Description = "UUIDNext.Uuid.NewDatabaseFriendly")]
    public Guid V7UuidNext() => Uuid.NewDatabaseFriendly(Database.PostgreSql);

    [Benchmark(Description = "Medo.Uuid7.NewGuid()")]
    public Guid V7Medo() => Uuid7.NewGuid();
}
