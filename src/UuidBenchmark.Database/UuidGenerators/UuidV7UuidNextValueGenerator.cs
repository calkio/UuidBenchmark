using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using UUIDNext;

namespace UuidBenchmark.Database.UuidGenerators;

public class UuidV7UuidNextValueGenerator : ValueGenerator<Guid>
{
    public override Guid Next(EntityEntry entry) => Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql);
    public override bool GeneratesTemporaryValues => false;
}
