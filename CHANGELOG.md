# Changelog

Changes to Qorel are listed here, roughly following
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

First release.

### Qorel

- Query models. Declare each field once with `model.Map(...)`, along with what
  clients can do with it: `Filterable()`, `Sortable()`, `Searchable()` and
  `IgnoreCase()`. Models are checked when you call `AddModel`, so mistakes like a
  missing id or an operator the field's type can't support fail at startup.
- Typed field handles (`Field`, `TextField`, `ComparableField`) for building
  filters in code that the compiler checks.
- Client requests (`QueryRequest`) are validated against the model, and every
  problem comes back as a `ValidationError` with a code from `ErrorCodes`, not an
  exception.
- `QueryBuilder` for queries built in your own code.
- Keyset pagination with URL-safe cursors. A cursor only works with the sorts it
  was made for. Nullable sort fields put nulls last ascending and first
  descending on every database.
- Default sorts and per-model page sizes with `model.DefaultSort(...)` and
  `model.PageSize(...)`.
- Free-text search over the fields marked `Searchable()`.
- Optional bounding-box filters with `model.SpatialFilter(...)`.
- `IServerFilter<TEntity>` for rules every query has to follow, like users only
  seeing their own items. The `serverFilters` parameter adds extra filters to a
  single call.
- Request limits in `QorelOptions`: page sizes, filters, filter values, sorts,
  search length and cursor length.
- `QueryModel.ParseFilters` and `ApplyFilters` for validating filters and
  running them in memory, with the same null handling as SQL.
- `FilterSet<TField>`, a list of filters with a `CacheKey` that stays the same
  whatever order the filters come in.
- `QueryModel.ClientFields`, for an endpoint that tells clients what they can
  filter and sort on.

### Qorel.EntityFramework

- `AddEntityFramework<TDbContext>()` registers an `IQueryPipeline` for every
  model, with `QueryAsync`, `CountAsync`, `FindAsync` and `ListAsync`. Queries
  are read-only and untracked, and every value is sent as a SQL parameter.

### Qorel.AspNetCore

- `ToHttpResult()` returns `200 OK`, or a `422` problem details response with
  every validation error, and documents both in OpenAPI.
- `QueryParameters<TField>` for GET endpoints that take the query from the
  query string.
- `QueryValidationExceptionHandler`, which turns a `QueryValidationException`
  into the same 422 response.

### Qorel.Testing and Qorel.Testing.Assertions

- `QueryRequestBuilder` and `FilterRequestFactory` for building client requests
  in tests.
- Fluent assertions for `QueryResult` and `Page`, built on AwesomeAssertions.
