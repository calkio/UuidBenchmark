using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace UuidBenchmark.Database.UuidGenerators;

/// <summary>Baseline — то, что EF Core делает по умолчанию для Guid PK.</summary>
public class UuidV4ValueGenerator : ValueGenerator<Guid>
{
    public override Guid Next(EntityEntry entry) => Guid.NewGuid();
    public override bool GeneratesTemporaryValues => false;
}
