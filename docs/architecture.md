# Architecture

How Qorel fits together, if you're working on it.

## Design rules

- **Client mistakes are results, bugs are exceptions.** Anything a client can get wrong comes back as a `ValidationError` in a `QueryResult`. Exceptions are only for programming mistakes, like a misconfigured model or a filter built in code on an unmapped field, and those show up at startup wherever possible.
- **Validate once, at the edge.** A request is checked against the model before it turns into a typed query. After that nothing has to check client input again.
- **Every value is a parameter.** Expression trees never put a filter or cursor value in a `ConstantExpression`, because EF Core would write it straight into the SQL.
- **SQL and in-memory filtering agree.** Every operator has a SQL version (`FilterExpressions`) and an in-memory version (`InMemoryFilters`) with the same null rules, and `InMemoryParityTests` checks they match.
- **The core doesn't depend on EF Core.** Validation, filter expressions, keyset paging and cursors all live in `Qorel` and produce plain `IQueryable` queries. A backend just runs them.
- **Small public surface.** Public types are in the `Qorel` namespace and everything else is `internal`. The filter hierarchy is closed, so users can't create filters Qorel doesn't know how to run.

## How a request flows

```
QueryRequest (JSON body)  or  QueryParameters (query string, Qorel.AspNetCore)  or  QueryBuilder (code)
   |
   v
QueryPlanner.Plan                       checks every part and collects every error
   |   +- RequestValidator               fields, operators, values, limits, search, spatial bounds
   |   |     \- MappedField.TryCreateFilter -> FieldValueJson    read a raw JSON value as the field's type
   |   \- KeysetPagination.Create        sort keys (requested, default, then id) and the decoded cursor
   v
QueryPlan                               a checked query, with any additional filters added
   |   Matching(source)                 filters + search + spatial bounds  -> used by pages and counts
   |   SelectPage(source, projection)   Matching -> KeysetPagination.SelectPage
   v
QueryPipeline (EF Core)                 runs SelectPage with ToListAsync, then QueryPlan.ToPage builds the next cursor
```

A `QueryBuilder` skips the client checks. Its filters are already typed, so the planner just checks that every field it
uses is mapped.

Every pipeline call starts from the entities the request is allowed to see, which is the entity set with every
registered `IServerFilter` applied. Pages, counts, finds and lists all start from there.

## Where things live

| Folder           | What's in it                                                                                                                                                                                                                                                     |
| ---------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Fields/`        | The typed field handles users declare: `Field`, `TextField`, `ComparableField`.                                                                                                                                                                                  |
| `Filters/`       | `Filter` and its one implementation `FieldFilter`, the operators and which types support them, `FilterSet`, and reading and formatting values.                                                                                                                   |
| `Model/`         | `QueryModel` and its builders. `MappedField` is one mapped field and does everything that needs its value type. `BuiltModel` is what `Configure` produces and can't change. `RequestValidator` checks client input. `ClientField` is what clients can do with a field. |
| `Querying/`      | What comes in (`QueryRequest`, `SortRequest`, `QueryBuilder`) and what it turns into: `QueryPlanner` makes a `QueryPlan`.                                                                                                                                        |
| `Paging/`        | `KeysetPagination` sorts, skips past the previous page and builds the next cursor. `SortKey` is one sort column, `Cursor` encodes and decodes cursors, and `PageRow` is one row read back.                                                                        |
| `Predicates/`    | Turning a filter into an expression tree (`FilterExpressions`) or a delegate (`InMemoryFilters`), free-text search (`TextSearch`), and shared expression helpers.                                                                                                |
| `Results/`       | `QueryResult`, `ValidationError`, `ErrorCodes`, `Page` and `ItemCount`.                                                                                                                                                                                          |
| `Serialization/` | The JSON converters for requests, and strict enum name matching.                                                                                                                                                                                                 |

Each file has one top-level type. Small private types that only help one class, like the JSON converters' inner
`Converter<T>`, are nested inside it. The one exception is `MappedField.cs`, which has the non-generic base and its
typed subclass together, since they're really one thing.

`Qorel.EntityFramework` is three files: the pipeline, which runs plans against the entity set asynchronously, the
registration extension, and `DbContextAccessor`, which lets one open-generic pipeline work for every model.

`Qorel.AspNetCore` has the HTTP pieces so the core never depends on ASP.NET Core: `ToHttpResult()` and the 422
`QueryValidationProblem`, `QueryParameters` for GET endpoints, and `QueryValidationExceptionHandler`.

## Why the typed/untyped split

The model keeps its fields in one dictionary keyed by the field enum, so it can only hold them through a non-generic base, `MappedField<TEntity, TField>`. The subclass `MappedField<TEntity, TField, TValue>` knows the value type, and that's where all the typed work happens: reading JSON values, building predicates, and sort and cursor values. Filters work the same way. `Filter<TField>` is what users hold, and `FieldFilter<TField, TValue>` has the typed values. When a filter is applied, its mapped field casts it to the matching `FieldFilter`. That's the only place a filter gets cast, and if the value types don't match it fails with a clear message.

## Adding an operator

1. Add it to `FilterOperator`, and to a group in `FilterOperators` if it fits in one.
2. Say which types support it in `FilterOperatorRules`, and what operand it takes in `GetOperand`.
3. Add a factory method to the right field handle.
4. Handle it in both `FilterExpressions` and `InMemoryFilters`.
5. Add it to `InMemoryParityTests` so SQL and memory get checked against each other.
