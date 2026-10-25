using Microsoft.EntityFrameworkCore;

namespace Qorel.EntityFramework.Tests.Fixtures;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();

            // SQLite doesn't have a decimal type and EF Core can't sort by one, so prices are stored as REAL.
            // Not needed on Postgres or SQL Server, which have proper decimals.
            entity.Property(e => e.Price).HasConversion<double>();
            entity.Property(e => e.CreatedAt).HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v));
        });
    }
}
