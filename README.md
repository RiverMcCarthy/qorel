# Qorel

Filtering, sorting and cursor pagination for .NET APIs, with type checking.

Most APIs end up with the same list endpoint code over and over: read the filters, check them, build the query, page it, return errors. Qorel does that part for you. You declare which fields clients can filter, sort and search on, and Qorel validates incoming requests against that and runs them through EF Core with keyset pagination.

- **Validated by default** - clients can only use the fields and operators you allow. A bad request comes back as a list of errors, not a 500.
- **Keyset pagination** - no `OFFSET`, so deep pages are as fast as the first one and pages don't shift when data changes. Nullable and text sort columns work on every database.
- **Parameterised SQL** - all values are sent as parameters, so EF Core and the database can reuse query plans.
- **Search** - a `search` box that matches each word against the fields you pick.
- **Server-side filters** - write tenant or ownership rules once as an `IServerFilter` and they get applied to every query. Clients can't see or remove them.
- **Sensible defaults** - default sort and page sizes per model, plus limits so one request can't build a huge query.
- **Three ways in** - a JSON body, query string parameters for cacheable GET endpoints, or a typed `QueryBuilder` from your own code.
- **Works in memory too** - run the same filters over data you've already loaded and get the same results.

## Packages

| Package                    | What it's for                                                                              |
| -------------------------- | ------------------------------------------------------------------------------------------ |
| `Qorel`                    | Fields, models, requests, validation, results and in-memory filtering. No EF Core needed. |
| `Qorel.EntityFramework`    | Runs queries with EF Core.                                                                 |
| `Qorel.AspNetCore`         | HTTP responses (`200`, or a `422` listing every error) and query string requests.          |
| `Qorel.Testing`            | Builds client requests in tests.                                                           |
| `Qorel.Testing.Assertions` | Fluent assertions for `QueryResult` and `Page`.                                            |

```bash
dotnet add package Qorel.EntityFramework
dotnet add package Qorel.AspNetCore
```

Needs .NET 8 and EF Core 9.

## Quick start

**1. Name your fields.** An enum gives each field the name clients use, and a typed handle per field lets you build filters in code that the compiler checks. `TextField` adds text filters and `ComparableField` adds range filters:

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<ProductField>))] // so OpenAPI lists the names clients send
public enum ProductField { Id, Name, Price, Category, CreatedAt, OwnerId }

public static class ProductFields
{
    public static readonly Field<ProductField, Guid> Id = new(ProductField.Id);
    public static readonly TextField<ProductField> Name = new(ProductField.Name);
    public static readonly ComparableField<ProductField, decimal> Price = new(ProductField.Price);
    public static readonly Field<ProductField, ProductCategory> Category = new(ProductField.Category);
    public static readonly ComparableField<ProductField, DateTimeOffset> CreatedAt = new(ProductField.CreatedAt);
    public static readonly Field<ProductField, Guid> OwnerId = new(ProductField.OwnerId);
}
```

**2. Describe the entity.** A query model maps each field to a property on the entity and says what clients can do with it. If you don't give a field any options, only your own code can use it:

```csharp
public sealed class ProductQueryModel : QueryModel<Product, ProductField>
{
    protected override void Configure(QueryModelBuilder<Product, ProductField> model)
    {
        model.Id(ProductFields.Id, e => e.Id);
        model.Map(ProductFields.Name, e => e.Name).Filterable().Sortable().Searchable().IgnoreCase();
        model.Map(ProductFields.Price, e => e.Price).Filterable().Sortable();
        model.Map(ProductFields.Category, e => e.Category).Filterable();
        model.Map(ProductFields.CreatedAt, e => e.CreatedAt).Filterable().Sortable();
        model.Map(ProductFields.OwnerId, e => e.OwnerId); // server-side filters only

        model.DefaultSort(ProductFields.CreatedAt, SortDirection.Desc);
    }
}
```

**3. Register it.** The model gets checked here, so if you've made a mistake in it the app fails at startup:

```csharp
builder.Services.AddQorel()
    .AddModel(new ProductQueryModel())
    .AddEntityFramework<AppDbContext>();
```

Rules that every query has to follow, like "users only see their own products", are server filters. Register them like any other service and they're applied to every query:

```csharp
public sealed class OwnProducts(ICurrentUser user) : IServerFilter<Product>
{
    public Expression<Func<Product, bool>> Predicate => product => product.OwnerId == user.Id;
}

builder.Services.AddScoped<IServerFilter<Product>, OwnProducts>();
```

**4. Query.** Inject `IQueryPipeline` and pass it the client's request:

```csharp
app.MapPost("/products/query", async (
    QueryRequest<ProductField> request,
    IQueryPipeline<Product, ProductField> pipeline,
    CancellationToken cancellationToken) =>
{
    QueryResult<Page<ProductDto>> result = await pipeline.QueryAsync(
        request,
        e => new ProductDto(e.Id, e.Name, e.Price),
        cancellationToken: cancellationToken);

    return result.ToHttpResult();
});
```

`ToHttpResult()` gives you a `200 OK` with the page, or a `422` problem details response with every error in the request. Both show up in OpenAPI without any extra setup.

The client sends:

```json
{
  "filters": [
    { "field": "Category", "operator": "Equals", "value": "Electronics" },
    { "field": "Price", "operator": "LessThanOrEquals", "value": 499.99 }
  ],
  "sorts": [{ "field": "Price", "direction": "Desc" }],
  "search": "wireless",
  "pageSize": 20
}
```

and gets a page of items back with a `nextCursor`. To get the next page, send the same request again with `"cursor": "<nextCursor>"`.

If you want a GET endpoint so browsers and CDNs can cache it, bind `QueryParameters` from the query string instead:

```csharp
app.MapGet("/products", async ([AsParameters] QueryParameters<ProductField> query, IQueryPipeline<Product, ProductField> pipeline) =>
    (await pipeline.QueryAsync(query, e => new ProductDto(e.Id, e.Name, e.Price))).ToHttpResult());
```

```
GET /products?filters=[{"field":"Category","operator":"Equals","value":"Electronics"}]&sort=price:desc&search=wireless&pageSize=20
```

## In code

If the query comes from your own code and not a client, build it with typed filters:

```csharp
var query = new QueryBuilder<ProductField>()
    .Where(ProductFields.Category.EqualTo(ProductCategory.Electronics), ProductFields.Price.LessThan(500m))
    .OrderByDescending(ProductFields.Price)
    .WithPageSize(50);

QueryResult<Page<ProductDto>> result = await pipeline.QueryAsync(query, e => new ProductDto(e.Id, e.Name, e.Price));
```

## Documentation

- [Defining a model](docs/models.md): fields, operators, sorting, search, case sensitivity and spatial filters
- [Querying](docs/querying.md): request formats, server filters, counts, results and errors, limits, and filtering in memory
- [Pagination](docs/pagination.md): how keyset pagination and cursors work
- [Testing](docs/testing.md): faking the pipeline and the test helper packages
- [Architecture](docs/architecture.md): how the library fits together, if you want to work on it

## License

MIT. See [LICENSE](LICENSE).
