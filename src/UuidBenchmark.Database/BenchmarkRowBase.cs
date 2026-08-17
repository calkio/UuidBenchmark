namespace UuidBenchmark.Database;

/// <summary>
/// Общая форма строки для всех 5 таблиц. Payload имитирует реалистичную
/// ширину строки (пустая таблица с одним uuid — не то, что вы получите
/// в проде), SequenceNo — просто для отладки/проверки порядка вставки.
/// </summary>
public abstract class BenchmarkRowBase
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Payload { get; set; } = string.Empty;
    public long SequenceNo { get; set; }
}
