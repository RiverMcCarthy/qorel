using System.Linq.Expressions;

namespace Qorel;

/// <summary>
/// Reads one page of a query with keyset pagination. The sort keys and where to start come already checked from
/// <see cref="QueryPlanner{TEntity,TField}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Keyset pagination finds the next page by looking for the items that sort after the last one the client saw, rather
/// than skipping a count of rows. Pages stay fast at any depth and do not shift when rows are added or removed.
/// </para>
/// <para>
/// A page is read in one query: sort, skip past the previous page, take one item more than the page size (to find out
/// whether there is another page), and read each item's sort values alongside it (to build the next cursor).
/// </para>
/// </remarks>
/// <param name="keys">The sort keys, ending with the id so no two items tie.</param>
/// <param name="previousPageEnd">The previous page's last value in each key, or null for the first page.</param>
/// <param name="pageSize">How many items a page holds.</param>
internal sealed class KeysetPagination<TEntity, TField>(
    IReadOnlyList<SortKey<TEntity, TField>> keys,
    IReadOnlyList<object?>? previousPageEnd,
    int pageSize)
    where TEntity : class
    where TField : struct, Enum
{
    /// <summary>
    /// Builds the query for the page: sorted, after the previous page, at most one item more than a page, and projected
    /// with each item's sort values.
    /// </summary>
    public IQueryable<PageRow<TResult>> SelectPage<TResult>(IQueryable<TEntity> matching, Expression<Func<TEntity, TResult>> projection)
    {
        IQueryable<TEntity> sorted = OrderBy(matching);

        if (previousPageEnd is not null)
            sorted = sorted.Where(KeysetPredicate.After(keys, previousPageEnd));

        return sorted
            .Take(pageSize + 1)
            .Select(WithSortValues(projection));
    }

    /// <summary>
    /// Turns the rows <see cref="SelectPage{TResult}"/> read into a page. The extra row only shows there is a next page,
    /// so it is dropped, and the cursor points at the last item that is kept.
    /// </summary>
    public Page<TResult> ToPage<TResult>(IReadOnlyList<PageRow<TResult>> rows)
    {
        bool hasNextPage = rows.Count > pageSize;

        return new Page<TResult>
        {
            Items = [.. rows.Take(pageSize).Select(row => row.Item)],
            NextCursor = hasNextPage ? Cursor.Encode(keys, rows[pageSize - 1].SortValues) : null,
        };
    }

    private IOrderedQueryable<TEntity> OrderBy(IQueryable<TEntity> source)
    {
        IOrderedQueryable<TEntity> sorted = keys[0].Field.OrderBy(source, keys[0].Descending);

        foreach (SortKey<TEntity, TField> key in keys.Skip(1))
            sorted = key.Field.ThenBy(sorted, key.Descending);

        return sorted;
    }

    // Wraps the projection so each row also carries its sort values, read in the same query.
    private Expression<Func<TEntity, PageRow<TResult>>> WithSortValues<TResult>(Expression<Func<TEntity, TResult>> projection)
    {
        ParameterExpression entity = projection.Parameters[0];

        NewArrayExpression sortValues = Expression.NewArrayInit(
            typeof(object),
            keys.Select(key => Expression.Convert(key.Field.Read(entity), typeof(object))));

        return Expression.Lambda<Func<TEntity, PageRow<TResult>>>(
            Expression.New(PageRow<TResult>.Constructor, projection.Body, sortValues),
            entity);
    }
}
