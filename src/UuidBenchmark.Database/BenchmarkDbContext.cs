using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using UuidBenchmark.Database.UuidGenerators;

namespace UuidBenchmark.Database;

/// <summary>
/// Каждая стратегия живёт в своей физической таблице — принципиально важно
/// для честного замера фрагментации индекса: если смешать v4 и v7 в одной
/// таблице, эффект от v7 просто "размажется" по общему индексу и картина
/// станет нечитаемой.
/// </summary>
public class BenchmarkDbContext : DbContext
{
    public DbSet<RowV4> RowsV4 => Set<RowV4>();
    public DbSet<RowV7BuiltIn> RowsV7BuiltIn => Set<RowV7BuiltIn>();
    public DbSet<RowV7UuidNext> RowsV7UuidNext => Set<RowV7UuidNext>();
    public DbSet<RowV7Medo> RowsV7Medo => Set<RowV7Medo>();
    public DbSet<RowV7DbSide> RowsV7DbSide => Set<RowV7DbSide>();

    public BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureCodeSide<RowV4, UuidV4ValueGenerator>(modelBuilder, "rows_v4");
        ConfigureCodeSide<RowV7BuiltIn, UuidV7BuiltInValueGenerator>(modelBuilder, "rows_v7_builtin");
        ConfigureCodeSide<RowV7UuidNext, UuidV7UuidNextValueGenerator>(modelBuilder, "rows_v7_uuidnext");
        ConfigureCodeSide<RowV7Medo, UuidV7MedoValueGenerator>(modelBuilder, "rows_v7_medo");

        // Эта стратегия — не client-side ValueGenerator, а DEFAULT на стороне
        // БД (SQL-функция uuid_generate_v7() из db-init). EF Core не будет
        // подставлять значение сам, а прочитает сгенерированное через RETURNING
        // после INSERT.
        modelBuilder.Entity<RowV7DbSide>(b =>
        {
            b.ToTable("rows_v7_dbside");
            b.HasKey(r => r.Id);
            b.Property(r => r.Id)
                .HasDefaultValueSql("uuid_generate_v7()")
                .ValueGeneratedOnAdd();
            b.Property(r => r.Payload).HasMaxLength(500);
        });
    }

    private static void ConfigureCodeSide<TEntity, TGenerator>(ModelBuilder modelBuilder, string tableName)
        where TEntity : BenchmarkRowBase
        where TGenerator : ValueGenerator<Guid>
    {
        modelBuilder.Entity<TEntity>(b =>
        {
            b.ToTable(tableName);
            b.HasKey(r => r.Id);
            b.Property(r => r.Id)
                .ValueGeneratedOnAdd()
                .HasValueGenerator<TGenerator>();
            b.Property(r => r.Payload).HasMaxLength(500);
        });
    }
}
