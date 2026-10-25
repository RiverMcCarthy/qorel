namespace Qorel.EntityFramework.Tests.Fixtures;

public class ProductEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public ProductCategory Category { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public double? Rating { get; set; }
    public string? Brand { get; set; }
    public bool InStock { get; set; }
    public bool? Featured { get; set; }
    public Guid SupplierId { get; set; }
}

public enum ProductCategory { Electronics, Clothing, Food }
