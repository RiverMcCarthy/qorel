# Qorel.Samples

A minimal ASP.NET Core Web API showing a complete Qorel integration.

## Running

```bash
dotnet run
```

Then open http://localhost:5000/swagger to browse and try the endpoints.

## Endpoints

### `GET /products/fields`

Returns the fields clients can filter and sort by, from `ProductQueryModel.ClientFields`.

### `GET /products`

The same query as `POST /products/query`, from query string parameters, so it can be cached:

```
GET /products?filters=[{"field":"Category","operator":"Equals","value":"Electronics"}]&sort=price:desc&search=headphones&pageSize=5
```

### `POST /products/query`

A filtered, sorted, cursor-paginated query. Example body:

```json
{
  "filters": [
    { "field": "Category", "operator": "Equals", "value": "Electronics" },
    { "field": "Price", "operator": "LessThanOrEquals", "value": 100 }
  ],
  "sorts": [{ "field": "Price", "direction": "Asc" }],
  "search": "wireless",
  "pageSize": 5
}
```

To fetch the next page, send the same body again with `"cursor": "<nextCursor from the response>"`.

### `POST /products/count`

Counts the products matching the same body, capped at 1000.

### `GET /products/{id}`

Returns one product by id, or `404`.

## What this shows

- Registering with `AddQorel().AddModel(...).AddEntityFramework<AppDbContext>()`
- A `QueryModel` declaring which fields clients can filter and sort, with `Name` searchable and case-insensitive, and a default sort of newest first
- A GET endpoint bound from the query string with `[AsParameters] QueryParameters<ProductField>`
- `QueryValidationExceptionHandler`, so `GetValueOrThrow()` failures become 422 responses
- Binding a `QueryRequest<ProductField>` from the body and validating it against the model
- Cursor-based keyset pagination through `Page<T>.NextCursor`
- `CountAsync` with a cap, and `FindAsync` by id
- `result.ToHttpResult()` to return a page, or a `422` problem listing every error, with both documented in OpenAPI
- String enums, so OpenAPI and responses use the same names clients send
