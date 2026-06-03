# Qorel

A type-safe query pipeline for .NET APIs.

Define once which fields clients can filter and sort by, and Qorel handles the rest. It accepts filter, sort, and pagination requests from clients, validates them against your schema, and translates them into efficient EF Core queries with cursor-based keyset pagination.

The goal is simple: stop rebuilding the same list-endpoint infrastructure for every API.

> **Early days.** The core libraries are in place and building. Tests, sample applications, and full documentation are still in progress.

## Features

- **Schema-first validation** - clients can only filter and sort by the fields you've declared. Anything else is rejected before it reaches the database.
- **Cursor-based keyset pagination** - deterministic and fast at any depth, no `OFFSET`.
- **Two ways in** - a JSON `QueryRequest` from a client, or a typed `QueryBuilder` in code.
- **Server-side filters** - mandatory predicates (access control, tenant isolation) the client can't see or override.
- **Spatial bounds** - viewport queries via `BoundingBox`, behind a small `ISpatialFilter` interface.

## Projects

- **`Qorel`** - the core library. Defines field schemas, query/filter/sort request models, and result types. It has no EF Core dependency, so it can live in your application or domain layer.
- **`Qorel.EntityFramework`** - executes Qorel queries against EF Core and returns cursor-paginated results.

## The idea

Start with an enum of the fields you want to expose, then declare a typed constant for each one. The field _type_ you pick is what gives you compile-time type-safe filters later. For example, a `ComparableField` lets you build `GreaterThan`, a `TextField` lets you build `Contains`, and so on:

```csharp
public enum ProductField { Id, Name, Price, Category, CreatedAt }

public static class ProductFields
{
    public static readonly Field<ProductField, Guid>                     Id        = new(ProductField.Id);
    public static readonly TextField<ProductField>                       Name      = new(ProductField.Name);
    public static readonly ComparableField<ProductField, decimal>        Price     = new(ProductField.Price);
    public static readonly EnumField<ProductField, ProductCategory>      Category  = new(ProductField.Category);
    public static readonly ComparableField<ProductField, DateTimeOffset> CreatedAt = new(ProductField.CreatedAt);
}
```

| Field type                      | For                        | Operators it adds                                       |
| ------------------------------- | -------------------------- | ------------------------------------------------------- |
| `Field<TKey, TValue>`           | Guid, bool, any value type | `Equals`, `IsOneOf`, `IsNotOneOf`                       |
| `TextField<TKey>`               | `string`                   | `Contains`, `StartsWith`, `EndsWith`                    |
| `ComparableField<TKey, TValue>` | numbers, dates             | `GreaterThan`, `LessThan` (and the `OrEquals` variants) |
| `EnumField<TKey, TEnum>`        | enums                      | `Equals`, `IsOneOf`, `IsNotOneOf`                       |

A schema then says which of those fields clients are actually allowed to filter and sort - every incoming request is checked against it:

```csharp
public class ProductFieldSchema : FieldSchema<ProductField>
{
    protected override IEnumerable<FieldMetadata<ProductField>> Define() =>
    [
        For(ProductFields.Id),
        For(ProductFields.Name).Filterable().Sortable(),
        For(ProductFields.Price).Filterable().Sortable(),
        For(ProductFields.Category).Filterable(),
        For(ProductFields.CreatedAt).Filterable().Sortable(),
    ];
}
```

A field map then connects each field to a selector over your EF Core entity - no marker
interfaces or base classes on the entity itself:

```csharp
public class ProductFieldMap : FieldMap<ProductEntity, ProductField>
{
    public override ProductField IdKey => ProductField.Id;

    protected override void Build(FieldMapBuilder<ProductEntity, ProductField> map) => map
        .Add(ProductFields.Id,        e => e.Id)
        .Add(ProductFields.Name,      e => e.Name)
        .Add(ProductFields.Price,     e => e.Price)
        .Add(ProductFields.Category,  e => e.Category)
        .Add(ProductFields.CreatedAt, e => e.CreatedAt);
}
```

Register the pieces once at startup:

```csharp
services.AddQorel();
services.AddQorelSchema<ProductFieldSchema, ProductField>();
services.AddQorelPipeline<ProductEntity, ProductField, ProductFieldMap, AppDbContext>();
```

From then on, the client POSTs a plain JSON `QueryRequest`:

```json
{
  "filters": [
    { "fieldKey": "Category", "operator": "Equals", "value": "Electronics" },
    { "fieldKey": "Price", "operator": "LessThanOrEquals", "value": 499.99 }
  ],
  "sorts": [{ "fieldKey": "Price", "direction": "Desc" }],
  "pageSize": 20,
  "cursor": null
}
```

and your handler hands it straight to the pipeline:

```csharp
QueryResult<Page<ProductDto>> result = await pipeline.QueryAsync(
    request, e => new ProductDto(e.Id, e.Name, e.Price, e.Category), ct: ct);
```

Qorel validates every filter and sort against the schema before it touches the database, turns
them into SQL through EF Core, and returns a `Page<ProductDto>` with a `NextCursor`.
Send that cursor back to get the next page.

## Building queries in code

Client JSON isn't the only way in. When the filters are yours - a background job, an internal
service, a test - you build them with `QueryBuilder` instead. The field constants give you
strongly-typed factory methods, so the value type is checked at compile time and there's no
schema-validation step to pay for:

```csharp
QueryBuilder<ProductField> query = QueryBuilder<ProductField>.Create()
    .Where(ProductFields.Category.Equals(ProductCategory.Electronics))
    .Where(ProductFields.Price.LessThanOrEquals(499.99m))
    .OrderByDescending(ProductField.Price)
    .ThenBy(ProductField.CreatedAt)
    .WithPageSize(20);

QueryResult<Page<ProductDto>> result = await pipeline.QueryAsync(
    query, e => new ProductDto(e.Id, e.Name, e.Price, e.Category), ct: ct);
```

Both entry points also take **server-side filters** - predicates you apply on top of whatever
the client asked for, and that the client can never see or switch off. This is how you can add
access control without trusting the request:

```csharp
// OwnerId is in the field map but deliberately left out of the schema, so a client can't
// filter on it, but the server always can.
IFilter<ProductField> ownerOnly = ProductFields.OwnerId.Equals(currentUserId);

QueryResult<Page<ProductDto>> result = await pipeline.QueryAsync(
    request, e => new ProductDto(e.Id, e.Name, e.Price, e.Category),
    serverFilters: [ownerOnly], ct: ct);
```

## How it works

A few of the design decisions, since they're the interesting part:

- **Keyset, not offset.** Paging uses an OR-of-ANDs keyset predicate across the sort columns,
  with the primary key appended as a tiebreaker so the ordering is always total. Pages don't
  drift or skip rows when data changes underneath you, and there's no `OFFSET` scan to slow
  down on deep pages.
- **One round-trip per page.** The sort-key values needed to build the _next_ cursor are
  projected in the same `SELECT` as the page itself, so producing a cursor costs no extra
  query.
- **No reflection on the hot path.** Filters, sorts and field access are all `Expression`
  trees, composed once and translated to SQL by EF Core. Reflection only happens at startup,
  when the schema and field map are first built (and then frozen).
- **Validation returns; it doesn't throw.** Invalid client input comes back as a
  `QueryResult` carrying structured `ValidationError`s (unknown field, operator not allowed,
  field not sortable etc.), so it can be mapped to a `422` instead of catching
  exceptions. Even a malformed or tampered cursor surfaces as a validation error, never a 500.
- **The core doesn't know about EF.** `Qorel` has no persistence dependency - the schema,
  request and result types stand on their own. `Qorel.EntityFramework` is simply the first
  execution backend.

## Requirements

.NET 8+ and EF Core 9.
