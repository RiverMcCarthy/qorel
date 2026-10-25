using Microsoft.EntityFrameworkCore;

namespace Qorel.Benchmarks.Fixtures;

public class BenchmarkProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public BenchmarkCategory Category { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public enum BenchmarkCategory { Electronics, Clothing, Food }

public enum BenchmarkField { Id, Name, Price, Stock, Category, CreatedAt }

public static class BenchmarkFields
{
    public static readonly Field<BenchmarkField, Guid> Id = new(BenchmarkField.Id);
    public static readonly TextField<BenchmarkField> Name = new(BenchmarkField.Name);
    public static readonly ComparableField<BenchmarkField, decimal> Price = new(BenchmarkField.Price);
    public static readonly ComparableField<BenchmarkField, int> Stock = new(BenchmarkField.Stock);
    public static readonly Field<BenchmarkField, BenchmarkCategory> Category = new(BenchmarkField.Category);
    public static readonly ComparableField<BenchmarkField, DateTimeOffset> CreatedAt = new(BenchmarkField.CreatedAt);
}

public sealed class BenchmarkQueryModel : QueryModel<BenchmarkProduct, BenchmarkField>
{
    protected override void Configure(QueryModelBuilder<BenchmarkProduct, BenchmarkField> model)
    {
        model.Id(BenchmarkFields.Id, e => e.Id);
        model.Map(BenchmarkFields.Name, e => e.Name).Filterable().Sortable();
        model.Map(BenchmarkFields.Price, e => e.Price).Filterable().Sortable();
        model.Map(BenchmarkFields.Stock, e => e.Stock).Filterable().Sortable();
        model.Map(BenchmarkFields.Category, e => e.Category).Filterable();
        model.Map(BenchmarkFields.CreatedAt, e => e.CreatedAt).Filterable().Sortable();
    }
}

public class BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : DbContext(options)
{
    public DbSet<BenchmarkProduct> Products => Set<BenchmarkProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BenchmarkProduct>(entity =>
        {
            entity.HasKey(e => e.Id);

            // SQLite can't sort decimals, so prices are stored as REAL.
            entity.Property(e => e.Price).HasConversion<double>();
            entity.Property(e => e.CreatedAt).HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v));
        });
    }
}
