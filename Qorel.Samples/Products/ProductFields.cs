using System.Text.Json.Serialization;

namespace Qorel.Samples.Products;

// Fields are sent by name, and this attribute makes OpenAPI show the names instead of numbers.
[JsonConverter(typeof(JsonStringEnumConverter<ProductField>))]
public enum ProductField { Id, Name, Price, Stock, Category, CreatedAt }

public static class ProductFields
{
    public static readonly Field<ProductField, Guid> Id = new(ProductField.Id);
    public static readonly TextField<ProductField> Name = new(ProductField.Name);
    public static readonly ComparableField<ProductField, decimal> Price = new(ProductField.Price);
    public static readonly ComparableField<ProductField, int> Stock = new(ProductField.Stock);
    public static readonly Field<ProductField, ProductCategory> Category = new(ProductField.Category);
    public static readonly ComparableField<ProductField, DateTimeOffset> CreatedAt = new(ProductField.CreatedAt);
}
