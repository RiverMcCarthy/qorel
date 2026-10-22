namespace Qorel.Testing;

/// <summary>
/// Builds a <see cref="QueryRequest{TField}"/> the way a client would send it, for tests of the client-facing path.
/// For queries built in code, use <see cref="QueryBuilder{TField}"/> instead.
/// </summary>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
public sealed class QueryRequestBuilder<TField>
    where TField : struct, Enum
{
    private readonly List<FilterRequest<TField>> _filters = [];
    private readonly List<SortRequest<TField>> _sorts = [];
    private string? _search;
    private string? _cursor;
    private int? _pageSize;
    private BoundingBox? _spatialBounds;

    /// <summary>
    /// Adds a filter with a single value.
    /// </summary>
    public QueryRequestBuilder<TField> Filter<TValue>(TField field, FilterOperator op, TValue value)
    {
        _filters.Add(FilterRequestFactory.Create(field, op, value));
        return this;
    }

    /// <summary>
    /// Adds an <c>IsOneOf</c> or <c>IsNotOneOf</c> filter.
    /// </summary>
    public QueryRequestBuilder<TField> FilterList<TValue>(TField field, FilterOperator op, IEnumerable<TValue> values)
    {
        _filters.Add(FilterRequestFactory.CreateList(field, op, values));
        return this;
    }

    /// <summary>
    /// Adds an <c>IsNull</c> or <c>IsNotNull</c> filter.
    /// </summary>
    public QueryRequestBuilder<TField> FilterNull(TField field, FilterOperator op)
    {
        _filters.Add(FilterRequestFactory.CreateNullCheck(field, op));
        return this;
    }

    /// <summary>
    /// Adds a sort.
    /// </summary>
    public QueryRequestBuilder<TField> Sort(TField field, SortDirection direction = SortDirection.Asc)
    {
        _sorts.Add(new SortRequest<TField> { Field = field, Direction = direction });
        return this;
    }

    /// <summary>
    /// Sets the search text.
    /// </summary>
    public QueryRequestBuilder<TField> WithSearch(string? search)
    {
        _search = search;
        return this;
    }

    /// <summary>
    /// Sets the cursor from a previous page.
    /// </summary>
    public QueryRequestBuilder<TField> WithCursor(string? cursor)
    {
        _cursor = cursor;
        return this;
    }

    /// <summary>
    /// Sets the requested page size.
    /// </summary>
    public QueryRequestBuilder<TField> WithPageSize(int? pageSize)
    {
        _pageSize = pageSize;
        return this;
    }

    /// <summary>
    /// Sets the spatial bounds.
    /// </summary>
    public QueryRequestBuilder<TField> WithSpatialBounds(BoundingBox? bounds)
    {
        _spatialBounds = bounds;
        return this;
    }

    /// <summary>
    /// Builds the request.
    /// </summary>
    public QueryRequest<TField> Build() => new()
    {
        Filters = [.. _filters],
        Sorts = [.. _sorts],
        Search = _search,
        Cursor = _cursor,
        PageSize = _pageSize,
        SpatialBounds = _spatialBounds,
    };
}
