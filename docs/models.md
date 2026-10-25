# Defining a model

A model says how an entity can be queried: what fields it has, how each one is read from the entity, and what clients are allowed to do with each one.

## Fields

Fields are named with an enum. The member names are what clients send, so they're part of your API and you should treat them that way.

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<ProductField>))]
public enum ProductField { Id, Name, Price, Stock, Category, CreatedAt, Rating, OwnerId }
```

Qorel can read field names from requests without the attribute. It's there so everything else matches: OpenAPI shows names instead of numbers, and anything your API returns that includes a field uses the names too. If clients filter on other enums, give them the same attribute so responses use the same names filters accept.

Then add a typed handle for each field. The handle type decides which filters you can build with it in code:

```csharp
public static class ProductFields
{
    public static readonly Field<ProductField, Guid> Id = new(ProductField.Id);
    public static readonly TextField<ProductField> Name = new(ProductField.Name);
    public static readonly ComparableField<ProductField, decimal> Price = new(ProductField.Price);
    public static readonly ComparableField<ProductField, int> Stock = new(ProductField.Stock);
    public static readonly Field<ProductField, ProductCategory> Category = new(ProductField.Category);
    public static readonly ComparableField<ProductField, DateTimeOffset> CreatedAt = new(ProductField.CreatedAt);
    public static readonly Field<ProductField, double?> Rating = new(ProductField.Rating);
    public static readonly Field<ProductField, Guid> OwnerId = new(ProductField.OwnerId);
}
```

| Handle                            | Use it for                                | Filters                                                                         |
| --------------------------------- | ----------------------------------------- | ------------------------------------------------------------------------------- |
| `Field<TField, TValue>`           | any type                                  | `EqualTo`, `IsOneOf`, `IsNotOneOf`, and `IsNull`/`IsNotNull` for nullable types |
| `TextField<TField>`               | `string`                                  | adds `Contains`, `StartsWith`, `EndsWith`                                       |
| `ComparableField<TField, TValue>` | numbers, dates, anything `IComparable<T>` | adds `GreaterThan`, `GreaterThanOrEquals`, `LessThan`, `LessThanOrEquals`       |

## The model

Inherit from `QueryModel<TEntity, TField>` and set up each field in `Configure`:

```csharp
public sealed class ProductQueryModel : QueryModel<Product, ProductField>
{
    protected override void Configure(QueryModelBuilder<Product, ProductField> model)
    {
        model.Id(ProductFields.Id, e => e.Id);
        model.Map(ProductFields.Name, e => e.Name).Filterable().Sortable().Searchable().IgnoreCase();
        model.Map(ProductFields.Price, e => e.Price).Filterable().Sortable();
        model.Map(ProductFields.Stock, e => e.Stock).Sortable();
        model.Map(ProductFields.Category, e => e.Category).Filterable();
        model.Map(ProductFields.CreatedAt, e => e.CreatedAt).Filterable().Sortable();
        model.Map(ProductFields.Rating, e => e.Rating).Filterable(FilterOperators.Comparison);
        model.Map(ProductFields.OwnerId, e => e.OwnerId);

        model.DefaultSort(ProductFields.CreatedAt, SortDirection.Desc);
    }
}
```

- **`Id`** maps the entity's unique id. Every model needs one. `FindAsync` uses it, and it's always the last sort so items with the same sort values stay in the same order across pages.
- **`Map`** maps any other field. The selector is usually just a property, but it can be any expression your LINQ provider can translate.
- **`Filterable()`** lets clients filter on the field with any operator its type supports. Pass operators to allow fewer, e.g. `Filterable(FilterOperators.Comparison)`.
- **`Sortable()`** lets clients sort by it. Only ordered types can be sorted (numbers, dates, strings, `bool`, `Guid` and their nullable versions), and the compiler enforces that.
- **`Searchable()`** matches a request's `search` text against the field. Text fields only. See [Search](#search).
- **`IgnoreCase()`** makes text filters and search ignore case. Text fields only. See [Case sensitivity](#case-sensitivity).
- **`DefaultSort`** sorts queries that don't ask for a sort. See [Default sort](#default-sort).
- **`PageSize`** sets the model's own default and max page sizes. See [Page sizes](#page-sizes).
- **A field with no options**, like `OwnerId` above, can only be used from your own code, e.g. in [server-side filters](querying.md#server-side-filters). Clients can't use it, and a request that names it gets rejected the same as if the field didn't exist, so they can't find out it's there either.

The entity doesn't need a base class or interface. A model can describe an EF Core entity, or just a plain record that you only filter in memory.

### Registering

```csharp
builder.Services.AddQorel()
    .AddModel(new ProductQueryModel())
    .AddModel(new OrderQueryModel())
    .AddEntityFramework<AppDbContext>();
```

Rules every query for a model has to follow are registered separately as [server filters](querying.md#server-side-filters).

`AddModel` builds the model straight away, so these mistakes throw at startup instead of on the first request:

- no `Id`, or the same field mapped twice
- an operator the field's type can't support, e.g. `Contains` on a number
- `Filterable()` on a type Qorel can't filter

Once it's built the model can't change, and it's registered as a singleton. You can inject it as `ProductQueryModel` or as `QueryModel<Product, ProductField>`.

## Operators by type

By default `Filterable()` allows:

| Field type                                                       | Operators                                                                                      |
| ---------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| `string`                                                         | `Equals`, `IsOneOf`, `IsNotOneOf`, `Contains`, `StartsWith`, `EndsWith`, `IsNull`, `IsNotNull` |
| `byte`, `short`, `int`, `long`, `float`, `double`, `decimal`     | `Equals`, `IsOneOf`, `IsNotOneOf`, and the four comparisons                                    |
| `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan` | `Equals`, `IsOneOf`, `IsNotOneOf`, and the four comparisons                                    |
| `bool`, `Guid`, enums                                            | `Equals`, `IsOneOf`, `IsNotOneOf`                                                              |
| nullable value types (`int?`, `Guid?`, ...)                      | the type's operators plus `IsNull` and `IsNotNull`                                             |

To narrow a field down, `FilterOperators` has these groups: `Equality`, `Comparison`, `Text` and `NullCheck`.

## Case sensitivity

By default Qorel passes text filters to the database as they are, so it's the database's collation that decides whether case matters. That varies a lot. SQL Server usually ignores case and Postgres doesn't. SQLite ignores case for `StartsWith` and `EndsWith` but not for `Equals` or `Contains`. In memory, text is always compared exactly.

For fields people search on, like names and titles, you usually want case ignored everywhere:

```csharp
model.Map(ProductFields.Name, e => e.Name).Filterable().Sortable().IgnoreCase();
```

With that, every text filter on the field ignores case on every database and in memory, so a filtered list from the database and the same filter run in memory always agree. It doesn't change sorting. Don't use it for codes and ids where case matters.

**Performance.** The database compares `LOWER(column)` against a lowercased value. `Contains` and `EndsWith` can't use a normal index anyway, so there's no extra cost for them. `Equals` and `IsOneOf` can't use the plain index on the column any more. You won't notice that on tables up to tens of thousands of rows. On a much bigger table, add an index on the lowercased column and the database will use it:

```sql
CREATE INDEX ix_products_name_lower ON products (lower(name));
```

Postgres and SQL Server lowercase any language. SQLite's `LOWER()` only handles A to Z, so an accented capital like `É` won't match `é` there.

## Sorting and nulls

If a sortable field can be null, like `double?` or `string?`, nulls go **last when ascending** and **first when descending** on every database. Databases don't agree on this by default, so Qorel adds an "is null" sort before the column to keep it consistent.

For text fields, Qorel uses the nullable annotation to tell whether the field can be null: `string Name` can't, `string? Nickname` can. If the column allows nulls, declare the property as nullable. Fields that can't be null sort on the plain column so its index can be used.

Index the columns clients can sort by, since pages read them in order.

## Default sort

If a query doesn't ask for a sort, the model's default sort is used, or the id if there isn't one. Call `DefaultSort` again to add a second sort for items that tie on the first:

```csharp
model.DefaultSort(ProductFields.CreatedAt, SortDirection.Desc)
     .DefaultSort(ProductFields.Name);
```

If the request has any sort, it replaces the default instead of adding to it. The field doesn't need to be `Sortable()`, because clients aren't choosing it, but it does need to be mapped.

## Page sizes

Page sizes come from `QorelOptions` unless the model sets its own. A model with small items, like points on a map, might want bigger pages than the rest of the API:

```csharp
model.PageSize(defaultSize: 200, maxSize: 1000);
```

You can leave either one out to keep the value from the options. If the default ends up bigger than the max, it throws at startup.

## Search

Mark the fields people search with `Searchable()` and a request's `search` text gets matched against them. Each word has to show up in at least one searchable field, so "red shoes" finds "Red running shoes" and "Shoes by Redwood":

```csharp
model.Map(ProductFields.Name, e => e.Name).Searchable().IgnoreCase();
model.Map(ProductFields.Brand, e => e.Brand).Searchable().IgnoreCase();
```

```sql
WHERE (lower(name) LIKE '%red%' OR lower(brand) LIKE '%red%')
  AND (lower(name) LIKE '%shoes%' OR lower(brand) LIKE '%shoes%')
```

Each word uses the field's own `Contains`, so it follows the field's `IgnoreCase()` setting and gets sent as a parameter. Use `IgnoreCase()` on searchable fields, otherwise the results depend on the database's collation.

Search uses `LIKE '%word%'`, which can't use a normal index. It's fine for thousands up to a few hundred thousand rows. Only mark the few fields people actually search. For millions of rows, or if you need results ranked by relevance, use your database's full-text search instead, through a server filter or your own query.

If a request has search text and the model has no searchable fields it gets `search.not_supported`, and if the text is longer than `MaxSearchLength` it gets `search.too_long`.

## Spatial filters

To let clients filter by a bounding box, give the model a spatial filter. It returns the condition for one box:

```csharp
model.SpatialFilter(box => e =>
    e.Latitude >= box.MinLatitude && e.Latitude <= box.MaxLatitude &&
    e.Longitude >= box.MinLongitude && e.Longitude <= box.MaxLongitude);
```

With NetTopologySuite and PostGIS it could look like:

```csharp
model.SpatialFilter(box =>
{
    Geometry envelope = new Envelope(box.MinLongitude, box.MaxLongitude, box.MinLatitude, box.MaxLatitude).ToGeometry();
    return e => e.Location != null && e.Location.Intersects(envelope);
});
```

Clients send the box as `spatialBounds`:

```json
{ "spatialBounds": { "minLatitude": 51.4, "minLongitude": -0.2, "maxLatitude": 51.6, "maxLongitude": 0.1 } }
```

A box with a coordinate out of range, or a min bigger than its max, gets `spatial.bounds_invalid`. If a model has no spatial filter, spatial bounds sent to it get `spatial.not_supported` instead of being silently ignored. In code, `BoundingBox.Create(...)` checks the coordinates when you create it.

## Telling clients what they can use

`ClientFields` lists every field clients can filter, sort or search on, as a `ClientField` with its operators. Server-only fields are left out. It's handy for a metadata endpoint:

```csharp
app.MapGet("/products/fields", (ProductQueryModel model) => model.ClientFields.Select(field => new
{
    field = field.Field.ToString(),
    filterable = field.IsFilterable,
    sortable = field.IsSortable,
    searchable = field.IsSearchable,
    operators = field.AllowedOperators.Select(op => op.ToString()),
}));
```
