using Microsoft.EntityFrameworkCore;

using Qorel.Samples.Products;

namespace Qorel.Samples.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            // SQLite doesn't have a decimal type and EF Core can't sort by one, so prices are stored as doubles.
            // Not needed on Postgres or SQL Server, which have proper decimals.
            entity.Property(e => e.Price).HasConversion<double>();
            entity.Property(e => e.CreatedAt).HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v));
        });
    }
}
