using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace UuidBenchmark.Database.UuidGenerators;

/// <summary>Встроенный в .NET 9+ генератор v7.</summary>
public class UuidV7BuiltInValueGenerator : ValueGenerator<Guid>
{
    public override Guid Next(EntityEntry entry) => Guid.CreateVersion7();
    public override bool GeneratesTemporaryValues => false;
}
