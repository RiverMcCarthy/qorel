namespace Qorel.Samples.Products;

public sealed class ProductQueryModel : QueryModel<ProductEntity, ProductField>
{
    protected override void Configure(QueryModelBuilder<ProductEntity, ProductField> model)
    {
        model.Id(ProductFields.Id, e => e.Id);
        model.Map(ProductFields.Name, e => e.Name).Filterable().Sortable().Searchable().IgnoreCase();
        model.Map(ProductFields.Price, e => e.Price).Filterable().Sortable();
        model.Map(ProductFields.Stock, e => e.Stock).Filterable().Sortable();
        model.Map(ProductFields.Category, e => e.Category).Filterable();
        model.Map(ProductFields.CreatedAt, e => e.CreatedAt).Filterable().Sortable();

        // Newest first unless the request sorts by something else.
        model.DefaultSort(ProductFields.CreatedAt, SortDirection.Desc);
    }
}
