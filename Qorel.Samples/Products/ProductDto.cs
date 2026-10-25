namespace Qorel.Samples.Products;

public record ProductDto(Guid Id, string Name, decimal Price, ProductCategory Category, int Stock);

public record ProductFieldInfo(ProductField Field, bool Filterable, bool Sortable, bool Searchable, IReadOnlyList<FilterOperator> Operators);
