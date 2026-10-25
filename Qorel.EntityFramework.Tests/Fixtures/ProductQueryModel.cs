namespace Qorel.EntityFramework.Tests.Fixtures;

public enum ProductField { Id, Name, Price, Stock, Category, CreatedAt, Rating, Brand, InStock, Featured, SupplierId }

public static class ProductFields
{
    public static readonly Field<ProductField, Guid> Id = new(ProductField.Id);
    public static readonly TextField<ProductField> Name = new(ProductField.Name);
    public static readonly ComparableField<ProductField, decimal> Price = new(ProductField.Price);
    public static readonly ComparableField<ProductField, int> Stock = new(ProductField.Stock);
    public static readonly Field<ProductField, ProductCategory> Category = new(ProductField.Category);
    public static readonly ComparableField<ProductField, DateTimeOffset> CreatedAt = new(ProductField.CreatedAt);
    public static readonly Field<ProductField, double?> Rating = new(ProductField.Rating);
    public static readonly TextField<ProductField> Brand = new(ProductField.Brand);
    public static readonly ComparableField<ProductField, bool> InStock = new(ProductField.InStock);
    public static readonly Field<ProductField, bool?> Featured = new(ProductField.Featured);
    public static readonly Field<ProductField, Guid> SupplierId = new(ProductField.SupplierId);
}

/// <summary>
/// The model most of the tests use. Pass <paramref name="searchable"/> to make Name and Brand searchable, and use
/// <paramref name="configure"/> for anything else a test needs, like a default sort.
/// </summary>
public sealed class ProductQueryModel(
    bool ignoreCase = false,
    bool searchable = false,
    Action<QueryModelBuilder<ProductEntity, ProductField>>? configure = null)
    : QueryModel<ProductEntity, ProductField>
{
    protected override void Configure(QueryModelBuilder<ProductEntity, ProductField> model)
    {
        model.Id(ProductFields.Id, e => e.Id).Sortable();
        model.Map(ProductFields.Price, e => e.Price).Filterable().Sortable();
        model.Map(ProductFields.Stock, e => e.Stock).Filterable().Sortable();
        model.Map(ProductFields.Category, e => e.Category).Filterable();
        model.Map(ProductFields.CreatedAt, e => e.CreatedAt).Filterable().Sortable();
        model.Map(ProductFields.Rating, e => e.Rating).Filterable().Sortable();
        model.Map(ProductFields.InStock, e => e.InStock).Filterable().Sortable();
        model.Map(ProductFields.Featured, e => e.Featured).Filterable().Sortable();
        model.Map(ProductFields.SupplierId, e => e.SupplierId);

        FieldBuilder<ProductEntity, ProductField, string> name = model.Map(ProductFields.Name, e => e.Name).Filterable().Sortable();
        FieldBuilder<ProductEntity, ProductField, string> brand = model.Map(ProductFields.Brand, e => e.Brand!).Filterable().Sortable();

        if (ignoreCase)
            name.IgnoreCase();

        if (searchable)
        {
            name.Searchable().IgnoreCase();
            brand.Searchable().IgnoreCase();
        }

        configure?.Invoke(model);
    }
}
