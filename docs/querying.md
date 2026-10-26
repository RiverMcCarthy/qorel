# Querying

Inject `IQueryPipeline<TEntity, TField>` to run queries. `AddEntityFramework<TDbContext>()` registers one for every model, scoped the same as your `DbContext`. All queries are read-only and untracked.

## Client requests

A client sends a `QueryRequest<TField>` as a POST body, or the same thing as [query string parameters](#get-endpoints):

```json
{
  "filters": [
    { "field": "Category", "operator": "Equals", "value": "Electronics" },
    { "field": "Price", "operator": "LessThanOrEquals", "value": 499.99 },
    { "field": "Name", "operator": "Contains", "value": "pro" },
    { "field": "Rating", "operator": "IsNull" }
  ],
  "sorts": [
    { "field": "Price", "direction": "Desc" },
    { "field": "CreatedAt" }
  ],
  "search": "wireless headphones",
  "pageSize": 20,
  "cursor": null
}
```

- Everything is optional. `{}` gets the first page of everything in the model's [default sort](models.md#default-sort).
- Field names, operators, directions and enum values ignore case. Enum values have to be sent by name.
- `IsOneOf` and `IsNotOneOf` take an array. `IsNull` and `IsNotNull` don't take a value.
- `direction` is `Asc` (the default) or `Desc`.
- `search` is matched against the model's [searchable fields](models.md#search). Each word has to show up in at least one of them.
- Values are read strictly: numbers have to be JSON numbers, and dates and Guids have to be strings. `"5"` for a number is an error, not 5.

Pass the request to the pipeline along with a projection, which runs in the database:

```csharp
QueryResult<Page<ProductDto>> result = await pipeline.QueryAsync(
    request,
    e => new ProductDto(e.Id, e.Name, e.Price),
    cancellationToken: cancellationToken);
```

The request gets checked against the model before anything hits the database. If something's wrong you get all the problems back in one go. See [Results and errors](#results-and-errors).

## GET endpoints

GET endpoints can't have a body, but browsers and CDNs can cache them and people can bookmark them. `QueryParameters<TField>` from `Qorel.AspNetCore` reads the same query from the query string:

```
GET /products?filters=[{"field":"Category","operator":"Equals","value":"Books"}]&sort=price:desc,name&search=red&pageSize=20&cursor=…
```

| Parameter  | Format                                                                         |
| ---------- | ------------------------------------------------------------------------------ |
| `filters`  | A JSON array of filters, same as in the body. URL-encode it.                   |
| `sort`     | Comma-separated sorts, each a field name with an optional `:asc` or `:desc`.   |
| `search`   | The search text.                                                               |
| `pageSize` | A number.                                                                      |
| `cursor`   | The `nextCursor` from the last page.                                           |
| `bounds`   | `minLatitude,minLongitude,maxLatitude,maxLongitude`, e.g. `51.4,-0.2,51.6,0.1` |

Bind it with `[AsParameters]`, which also documents each parameter in OpenAPI, and pass it to the pipeline:

```csharp
app.MapGet("/products", async (
    [AsParameters] QueryParameters<ProductField> query,
    IQueryPipeline<Product, ProductField> pipeline,
    CancellationToken cancellationToken) =>
{
    QueryResult<Page<ProductDto>> result = await pipeline.QueryAsync(query, e => new ProductDto(e.Id, e.Name, e.Price), cancellationToken: cancellationToken);
    return result.ToHttpResult();
});
```

In a controller, use `[FromQuery]`. If a parameter can't be read, like broken filter JSON or a sort on a field that doesn't exist, it comes back as a validation error like any other mistake. `CountAsync` takes `QueryParameters` too.

## Queries built in code

When the query comes from your own code, like a background job or an internal service, build it with `QueryBuilder<TField>` and the field handles. The compiler checks the filter values, and the client rules don't apply, so you can filter and sort on any mapped field:

```csharp
var query = new QueryBuilder<ProductField>()
    .Where(ProductFields.Category.EqualTo(ProductCategory.Electronics))
    .Where(ProductFields.Price.LessThan(500m))
    .OrderByDescending(ProductFields.Price)
    .ThenBy(ProductFields.Name)
    .WithSearch("wireless")
    .WithPageSize(10)
    .WithCursor(cursor);

QueryResult<Page<ProductDto>> result = await pipeline.QueryAsync(query, e => new ProductDto(e.Id, e.Name, e.Price));
```

Same as LINQ, `OrderBy` and `OrderByDescending` replace any sorts before them, and `ThenBy` and `ThenByDescending` add to them. With no sort, the model's default sort is used. Using a field the model doesn't map, or searching a model with no searchable fields, throws, because that's a bug in your code rather than bad input.

## Server-side filters

Server filters are rules added on top of whatever the client asks for, and the client can't see or remove them. Use them for ownership, visibility and other access rules.

**Rules every query needs** are classes that implement `IServerFilter<TEntity>`. The predicate is a normal expression, so it can use any property on the entity, navigation properties included, and combine conditions with `||`:

```csharp
public sealed class VisibleProducts(ICurrentUser user) : IServerFilter<Product>
{
    public Expression<Func<Product, bool>> Predicate => user.IsAdmin
        ? product => true
        : product => product.OwnerId == user.Id || product.Shares.Any(share => share.UserId == user.Id);
}

builder.Services.AddScoped<IServerFilter<Product>, VisibleProducts>();
```

Register them as scoped so they can use the current user. You can register as many as you like, and an item has to match all of them. They're applied to every pipeline call: `QueryAsync`, `CountAsync`, `FindAsync` and `ListAsync`. That means no endpoint can forget them, and a user can't get to someone else's item by guessing its id. Values the predicate uses, like `user.Id`, are sent as SQL parameters.

If all you need is tenant isolation across the whole app, EF Core's global query filters (`HasQueryFilter`) are worth a look too, since they apply to every EF query and not just Qorel's.

**Rules for a single call** can be passed as `additionalFilters`. They're added on top of the registered ones:

```csharp
QueryResult<Page<ProductDto>> result = await pipeline.QueryAsync(
    request,
    e => new ProductDto(e.Id, e.Name, e.Price),
    additionalFilters: [ProductFields.Status.EqualTo(ProductStatus.Published)],
    cancellationToken: cancellationToken);
```

A `QueryBuilder` query doesn't take them. It's already built in your code, so add the filters to it with `Where`.

## Counting

When the UI needs to show how many items match ("128 products"), use `CountAsync`. It takes the same request as the list, so a client can send the same body to both endpoints and the numbers always line up. Sorts and page size are ignored. The cursor is still checked though, so an invalid one gets `pagination.cursor_invalid` here as well.

```csharp
QueryResult<ItemCount> result = await pipeline.CountAsync(request, maxCount: 1000);

string label = result.Value.IsCapped ? $"{result.Value.Count}+" : $"{result.Value.Count}";
```

Counts are separate from pages on purpose. Putting a total on every page would mean a full `COUNT(*)` on every request, which throws away a lot of what keyset pagination gets you. Only ask for a count when you're going to show it.

Even one count can be slow on a big table, so `maxCount` puts a cap on it. The database stops once it's found one more than the cap. The cap is set in your code, not the request, so clients can't turn it off.

## Find by id

```csharp
ProductDto? product = await pipeline.FindAsync(id, e => new ProductDto(e.Id, e.Name, e.Price));
```

`id` has to be the same type the model's id is mapped as. If nothing matches you get the default (`null` for classes), and that includes when the item exists but a server filter hides it.

## Unpaged lists

When you need every matching row, e.g. for an export or an aggregation, use `ListAsync`. Registered server filters still apply. It's on you to make sure the result isn't huge:

```csharp
IReadOnlyList<ProductSummary> rows = await pipeline.ListAsync(
    [ProductFields.Category.EqualTo(ProductCategory.Electronics)],
    e => new ProductSummary(e.Id, e.Price));
```

## Results and errors

Every query returns a `QueryResult<T>`, which is either a value or the validation errors that stopped it. Client mistakes never throw.

In an ASP.NET Core endpoint, `ToHttpResult()` from `Qorel.AspNetCore` turns it into a response:

```csharp
return result.ToHttpResult();                                    // 200 OK with the value
return result.ToHttpResult(page => TypedResults.Ok(page.Items)); // or your own success response
```

Either way, a failure is a `422 Unprocessable Entity` with `application/problem+json`, with all the errors under `errors`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.21",
  "title": "The query is invalid.",
  "status": 422,
  "errors": [
    { "code": "field.not_sortable", "message": "Field 'Category' cannot be used in a sort.", "field": "Category" }
  ]
}
```

In a minimal API the return type tells OpenAPI about both responses, so you don't need any `.Produces(...)` calls. Controllers can return the result as an `IResult`.

Outside ASP.NET Core, or if you need something different:

```csharp
// Branch on it
return result.Match(onSuccess: page => Show(page), onFailure: errors => ShowErrors(errors));

// Check it yourself
if (result.IsFailure)
    return ShowErrors(result.Errors);

// Or, where invalid input means a bug, throw a QueryValidationException
Page<ProductDto> page = result.GetValueOrThrow();
```

`Map` transforms a successful value, and `Bind` chains on another step that can fail.

Each `ValidationError` has a `Code`, a `Message` meant for developers, and the `Field` it's about (or `null`). Match on the code using the `ErrorCodes` constants, since messages might change. Each constant is named after its code, e.g. `field.unknown` is `ErrorCodes.FieldUnknown`.

| Code                        | Cause                                                                                                                     |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| `field.unknown`             | The field doesn't exist, or it's server-side only                                                                         |
| `field.not_filterable`      | The field can't be filtered                                                                                               |
| `field.not_sortable`        | The field can't be sorted                                                                                                 |
| `operator.not_allowed`      | The field doesn't allow that operator                                                                                     |
| `filter.value_invalid`      | The value can't be read as the field's type, e.g. a string for a number, an unknown enum name, or one value for `IsOneOf` |
| `filter.too_many_values`    | An `IsOneOf` or `IsNotOneOf` has more values than `MaxFilterValues`                                                       |
| `filter.too_many`           | More filters than `MaxFilters`                                                                                            |
| `filter.malformed`          | The filters couldn't be read, e.g. broken JSON or a null entry                                                            |
| `sort.too_many`             | More sorts than `MaxSorts`                                                                                                |
| `sort.duplicate_field`      | The same field is sorted more than once                                                                                   |
| `sort.malformed`            | A sort couldn't be read, e.g. a null entry, or `price:up` in a query string                                               |
| `search.not_supported`      | The request has `search` text but the model has no searchable fields                                                      |
| `search.too_long`           | The search text is longer than `MaxSearchLength`                                                                          |
| `pagination.cursor_invalid` | The cursor can't be read, was changed, is too long, or was made for different sorts                                       |
| `spatial.bounds_invalid`    | A coordinate is out of range, or a min is bigger than its max                                                             |
| `spatial.not_supported`     | The request has `spatialBounds` but the model has no spatial filter                                                       |

If a request isn't valid JSON at all, or names a field or operator that doesn't exist, it fails when ASP.NET Core binds it, before Qorel ever sees it.

## Options and limits

```csharp
builder.Services.AddQorel(options =>
{
    options.DefaultPageSize = 20;   // when a request doesn't ask for a size
    options.MaxPageSize = 100;      // bigger requests get lowered to this
    options.MaxFilters = 50;        // per request
    options.MaxFilterValues = 500;  // per IsOneOf or IsNotOneOf
    options.MaxSorts = 5;           // per request
    options.MaxSearchLength = 100;  // characters
    options.MaxCursorLength = 4096; // characters
});
```

These are the defaults. The limits are there so one request can't build a massive SQL statement. A request over a limit gets an error instead of being trimmed, so the client knows its query didn't run the way it sent it. Page size is the exception, which just gets lowered.

`QueryBuilder` doesn't have to stick to these limits since it's your own code. `WithPageSize` throws if the size is less than 1.

## Unexpected errors

`GetValueOrThrow()` throws a `QueryValidationException` if the query is invalid. To turn that into the same 422 response `ToHttpResult()` gives, register the handler from `Qorel.AspNetCore`:

```csharp
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<QueryValidationExceptionHandler>();

app.UseExceptionHandler();
```

## Custom queries

For something the pipeline doesn't cover, like an aggregation, you can still validate client filters and apply them with the model:

```csharp
public async Task<IResult> Handle(ClusterQuery query, ProductQueryModel model, AppDbContext db)
{
    QueryResult<FilterSet<ProductField>> filters = model.ParseFilters(query.Filters);
    if (filters.IsFailure)
        return new QueryValidationProblem(filters.Errors);

    var clusters = await model.ApplyFilters(db.Products, filters.Value)
        .GroupBy(e => e.Category)
        .Select(g => new { g.Key, Count = g.Count() })
        .ToListAsync();

    return Results.Ok(clusters);
}
```

## Filters on their own

If you only need to validate a list of filters, e.g. for a map tile endpoint that just takes a `filters` parameter, use `ParseFilters`. It takes filter requests, or the JSON array as a string:

```csharp
app.MapGet("/routes/tiles/{z}/{x}/{y}", (int z, int x, int y, string? filters, RouteQueryModel model) =>
{
    QueryResult<FilterSet<RouteField>> result = model.ParseFilters(filters);
    if (result.IsFailure)
        return new QueryValidationProblem(result.Errors);

    // ...
});
```

No parameter, an empty string or `null` all mean no filters. JSON that can't be read comes back as `filter.malformed`.

## Filter sets and cache keys

`ParseFilters` returns a `FilterSet<TField>`, which is a list of filters with a `CacheKey` that's the same for the same filters no matter what order they came in.

```csharp
FilterSet<RouteField> filters = model.ParseFilters(json).Value;
filters.CacheKey; // Distance.LessThan=5000;Type.Equals="Hike"
```

The key doesn't depend on culture, and values that mean the same thing give the same key. `5000` and `5000.0` match, and so do two dates at the same instant in different offsets. Duplicate filters are dropped and `IsOneOf` values are sorted. Text values are written as JSON strings, so a value can't pretend to be another filter.

Two sets with the same key are equal, so you can use a set as a dictionary key:

```csharp
private readonly ConcurrentDictionary<FilterSet<RouteField>, PinIndex> _indexes = new();

PinIndex index = _indexes.GetOrAdd(filters, f => new PinIndex(model.ApplyFilters(points, f)));
```

For filters built in code, use `FilterSet<TField>.Create(...)`. Each filter also has its own `CacheKey`, and filters with the same key are equal.

Keys keep whatever case the client sent. On an `IgnoreCase()` field, "Ridge" and "ridge" match the same items but have different keys.

## Filtering in memory

If the data's already loaded, you can filter it with the same filters you'd send to the database:

```csharp
IEnumerable<RoutePoint> visible = model.ApplyFilters(points, filters);
```

This doesn't need EF Core, so the model can describe a plain record. Each selector gets compiled once, the first time that field is filtered in memory.

The results match the database, and there's a test that runs every operator both ways to make sure. The differences:

- **Text.** In memory, text is compared ordinally, so it's case-sensitive. In the database it depends on the collation. `IgnoreCase()` makes both ignore case.
- **Nulls** work like they do in SQL. A null never matches a comparison or a text filter, and it never throws. `EqualTo(null)`, `IsOneOf` and `IsNotOneOf` follow EF Core's null handling, which is the same as C#'s.
