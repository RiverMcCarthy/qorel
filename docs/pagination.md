# Pagination

Qorel uses **keyset** pagination, not offsets. So instead of "skip 1,000 rows and take 20", the next page is "the 20 items that come after the last one I saw". The database can jump straight there using an index, so page 50 is as quick as page 1. Pages also don't drift: if items get added or removed while someone is paging, they won't see the same row twice or skip one.

## Using it

Each page comes back with a `nextCursor`. Send the same request with that cursor to get the next page. When `nextCursor` is `null` you're on the last page.

```csharp
string? cursor = null;
do
{
    QueryResult<Page<ProductDto>> page = await pipeline.QueryAsync(request with { Cursor = cursor }, projection);
    // ... use page.Value.Items
    cursor = page.Value.NextCursor;
}
while (cursor is not null);
```

| `Page<T>` property | What it is                                           |
| ------------------ | ---------------------------------------------------- |
| `Items`            | The items on this page                               |
| `NextCursor`       | Cursor for the next page, or `null` on the last one  |
| `HasNextPage`      | `true` if there's another page                       |
| `Count`            | How many items are on this page                      |
| `IsEmpty`          | `true` if the page has no items                      |

Pages don't include a total. Use [`CountAsync`](querying.md#counting) if you need one.

## How it works

The pipeline sorts by the fields in the request (or the model's [default sort](models.md#default-sort) if there aren't any), and then by the id so no two items are ever tied. To get the items after the last one it adds:

```sql
WHERE (price > @p0)
   OR (price = @p0 AND id > @p1)
ORDER BY price, id
LIMIT 21
```

It asks for one more item than the page size. If that extra item comes back there's another page, and the cursor is built from the last item on this one. The sort values come back in the same `SELECT` as the page, so the cursor doesn't need another query. All the values are parameters, so the database uses the same plan for every page.

For each sort column the cursor stores the last row's value and a direction: `>` for ascending and `<` for descending.

## Cursors

A cursor is URL-safe base64 of a small JSON document holding the sorts it was made for and the last item's sort values.

- **A cursor only works with the sorts it was made for.** With different sorts its values wouldn't mean anything, so it's rejected with `pagination.cursor_invalid`. Filters aren't stored in it, so you can change filters between pages and the next page carries on from the same spot.
- **Cursors aren't signed.** Editing one only changes where the next page starts. The query's filters, server filters included, still apply, so a client can't use it to see anything it couldn't already page to.
- **Cursors don't expire.**
- **A cursor that's malformed, badly edited or longer than `MaxCursorLength`** gets `pagination.cursor_invalid`, not a 500.

## Nulls

A nullable sort field puts nulls **last when ascending** and **first when descending** on every database, and paging handles them fine. See [Sorting and nulls](models.md#sorting-and-nulls).

## Indexes

Keyset paging is only fast if the database can read the sort straight from an index. For each sort clients use, the best index is the sort columns followed by the id, e.g. `(price, id)` for sorting by price.

## SQLite and decimals

SQLite doesn't have a decimal type, and EF Core can't sort or compare a `decimal` column on it. If you sort or page by a decimal on SQLite, store it as a `double`:

```csharp
entity.Property(e => e.Price).HasConversion<double>();
```

Don't store it as `TEXT`, because text sorts `"129.99"` before `"34.99"`. You don't need this on Postgres or SQL Server.
