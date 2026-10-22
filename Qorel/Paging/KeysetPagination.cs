using System.Linq.Expressions;

namespace Qorel;

/// <summary>
/// Keyset pagination for one query: the sort keys, where the previous page ended, and how many items a page holds.
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
internal sealed class KeysetPagination<TEntity, TField>
    where TEntity : class
    where TField : struct, Enum
{
    private readonly IReadOnlyList<SortKey<TEntity, TField>> _keys;
    private readonly IReadOnlyList<object?>? _previousPageEnd;
    private readonly int _pageSize;

    private KeysetPagination(IReadOnlyList<SortKey<TEntity, TField>> keys, IReadOnlyList<object?>? previousPageEnd, int pageSize)
    {
        _keys = keys;
        _previousPageEnd = previousPageEnd;
        _pageSize = pageSize;
    }

    /// <summary>
    /// Sets up pagination for <paramref name="sorts"/>, starting after <paramref name="cursor"/>, or at the first page
    /// when there is no cursor. Fails when the cursor cannot be read or was made for different sorts.
    /// </summary>
    public static QueryResult<KeysetPagination<TEntity, TField>> Create(
        BuiltModel<TEntity, TField> model,
        IReadOnlyList<Sort<TField>> sorts,
        string? cursor,
        int pageSize)
    {
        IReadOnlyList<SortKey<TEntity, TField>> keys = SortKeys(model, sorts);

        if (string.IsNullOrEmpty(cursor))
            return QueryResult<KeysetPagination<TEntity, TField>>.Success(new(keys, previousPageEnd: null, pageSize));

        return Cursor.TryDecode(cursor, keys, model.Options.MaxCursorLength, out object?[] previousPageEnd)
            ? QueryResult<KeysetPagination<TEntity, TField>>.Success(new(keys, previousPageEnd, pageSize))
            : QueryResult<KeysetPagination<TEntity, TField>>.Failure(ValidationError.CursorInvalid());
    }

    /// <summary>
    /// Builds the query for the page: sorted, after the previous page, at most one item more than a page, and projected
    /// with each item's sort values.
    /// </summary>
    public IQueryable<PageRow<TResult>> SelectPage<TResult>(IQueryable<TEntity> matching, Expression<Func<TEntity, TResult>> projection)
    {
        IQueryable<TEntity> sorted = OrderBy(matching);

        if (_previousPageEnd is not null)
            sorted = sorted.Where(After(_previousPageEnd));

        return sorted
            .Take(_pageSize + 1)
            .Select(WithSortValues(projection));
    }

    /// <summary>
    /// Turns the rows <see cref="SelectPage{TResult}"/> read into a page. The extra row only shows there is a next page,
    /// so it is dropped, and the cursor points at the last item that is kept.
    /// </summary>
    public Page<TResult> ToPage<TResult>(IReadOnlyList<PageRow<TResult>> rows)
    {
        bool hasNextPage = rows.Count > _pageSize;

        return new Page<TResult>
        {
            Items = [.. rows.Take(_pageSize).Select(row => row.Item)],
            NextCursor = hasNextPage ? Cursor.Encode(_keys, rows[_pageSize - 1].SortValues) : null,
        };
    }

    // The id goes last, unless it is already a sort key, so no two items tie and nothing is skipped or repeated.
    private static List<SortKey<TEntity, TField>> SortKeys(BuiltModel<TEntity, TField> model, IReadOnlyList<Sort<TField>> sorts)
    {
        List<SortKey<TEntity, TField>> keys = [.. sorts.Select(sort => new SortKey<TEntity, TField>(model.GetField(sort.Field), sort.Descending))];

        if (!keys.Exists(key => key.Field == model.Id))
            keys.Add(new SortKey<TEntity, TField>(model.Id, Descending: false));

        return keys;
    }

    private IOrderedQueryable<TEntity> OrderBy(IQueryable<TEntity> source)
    {
        IOrderedQueryable<TEntity> sorted = _keys[0].Field.OrderBy(source, _keys[0].Descending);

        foreach (SortKey<TEntity, TField> key in _keys.Skip(1))
            sorted = key.Field.ThenBy(sorted, key.Descending);

        return sorted;
    }

    /// <summary>
    /// Matches the items that sort after <paramref name="previous"/>, the last item's value in each sort key.
    /// </summary>
    /// <remarks>
    /// An item comes after if it is after in the first key, or tied on the first and after in the second, and so on:
    /// <code>
    /// (k0 &gt; v0) OR (k0 = v0 AND k1 &gt; v1) OR (k0 = v0 AND k1 = v1 AND k2 &gt; v2)
    /// </code>
    /// </remarks>
    private Expression<Func<TEntity, bool>> After(IReadOnlyList<object?> previous)
    {
        ParameterExpression entity = Expression.Parameter(typeof(TEntity), "entity");

        Expression? after = null;
        Expression? tiedSoFar = null;

        for (int i = 0; i < _keys.Count; i++)
        {
            SortKey<TEntity, TField> key = _keys[i];
            Expression value = key.Field.Read(entity);

            // One parameter per key, used by both comparisons. Null when the previous item's value was null.
            Expression? previousValue = previous[i] is null ? null : key.Field.Parameter(previous[i]);

            if (IsAfter(key, value, previousValue) is { } isAfter)
                after = Or(after, And(tiedSoFar, isAfter));

            tiedSoFar = And(tiedSoFar, Expression.Equal(value, previousValue ?? QueryExpressions.Null(value.Type)));
        }

        return Expression.Lambda<Func<TEntity, bool>>(after ?? Expression.Constant(false), entity);
    }

    // The condition for an item being strictly after the previous one in this key, or null if no item can be.
    // Nulls sort last ascending and first descending (see MappedField.OrderBy), which decides where they fall here.
    private static Expression? IsAfter(SortKey<TEntity, TField> key, Expression value, Expression? previousValue)
    {
        if (previousValue is null)
        {
            // Ascending, nothing comes after the nulls at the end. Descending, every value comes after the nulls at the start.
            return key.Descending ? Expression.NotEqual(value, QueryExpressions.Null(value.Type)) : null;
        }

        Expression isAfter = QueryExpressions.Compare(key.Descending ? ExpressionType.LessThan : ExpressionType.GreaterThan, value, previousValue);

        // Ascending, the nulls are still to come after every value.
        return key.Field.CanBeNull && !key.Descending
            ? Expression.OrElse(isAfter, Expression.Equal(value, QueryExpressions.Null(value.Type)))
            : isAfter;
    }

    // Wraps the projection so each row also carries its sort values, read in the same query.
    private Expression<Func<TEntity, PageRow<TResult>>> WithSortValues<TResult>(Expression<Func<TEntity, TResult>> projection)
    {
        ParameterExpression entity = projection.Parameters[0];

        NewArrayExpression sortValues = Expression.NewArrayInit(
            typeof(object),
            _keys.Select(key => Expression.Convert(key.Field.Read(entity), typeof(object))));

        return Expression.Lambda<Func<TEntity, PageRow<TResult>>>(
            Expression.New(PageRow<TResult>.Constructor, projection.Body, sortValues),
            entity);
    }

    private static Expression And(Expression? left, Expression right) => left is null ? right : Expression.AndAlso(left, right);

    private static Expression Or(Expression? left, Expression right) => left is null ? right : Expression.OrElse(left, right);
}
