using System.Text.Json.Serialization;

namespace Qorel.Samples.Products;

public class ProductEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public ProductCategory Category { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// Filters use the category name, so send the name back in responses too.
[JsonConverter(typeof(JsonStringEnumConverter<ProductCategory>))]
public enum ProductCategory { Electronics, Clothing, Food, Books }
