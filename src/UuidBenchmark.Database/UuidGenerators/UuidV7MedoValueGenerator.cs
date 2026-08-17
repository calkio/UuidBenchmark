using Medo;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace UuidBenchmark.Database.UuidGenerators;

public class UuidV7MedoValueGenerator : ValueGenerator<Guid>
{
    public override Guid Next(EntityEntry entry) => Uuid7.NewGuid();
    public override bool GeneratesTemporaryValues => false;
}
