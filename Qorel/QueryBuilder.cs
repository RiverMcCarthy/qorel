using Qorel.Filters;
using Qorel.Querying;
using Qorel.Spatial;

namespace Qorel;

/// <summary>
/// Fluent builder for constructing a query programmatically where filters
/// are already strongly typed rather than deserialised from a client request.
/// </summary>
/// <remarks>
/// Pass the builder to <c>QueryPipeline.QueryAsync(builder, …)</c>.
/// Schema validation is skipped because the caller defines typed <see cref="IFilter{TKey}"/> instances directly.
/// </remarks>
/// <typeparam name="TKey">Enum type identifying fields in the schema.</typeparam>
public sealed class QueryBuilder<TKey> where TKey : Enum
{
    private readonly List<IFilter<TKey>> _filters = [];
    private readonly List<Sort<TKey>> _sorts = [];
    private string? _cursor;
    private int _pageSize = PaginationDefaults.Default;
    private BoundingBox? _spatialBounds;

    private QueryBuilder() { }

    /// <summary>
    /// Creates a new empty builder.
    /// </summary>
    public static QueryBuilder<TKey> Create() => new();

    /// <summary>
    /// Adds a single typed filter condition.
    /// </summary>
    public QueryBuilder<TKey> Where(IFilter<TKey> filter)
    {
        _filters.Add(filter);
        return this;
    }

    /// <summary>
    /// Adds multiple typed filter conditions.
    /// </summary>
    public QueryBuilder<TKey> Where(params IFilter<TKey>[] filters)
    {
        _filters.AddRange(filters);
        return this;
    }

    /// <summary>
    /// Appends a sort clause in the specified direction.
    /// </summary>
    public QueryBuilder<TKey> OrderBy(TKey field, SortDirection direction = SortDirection.Asc)
    {
        _sorts.Add(new Sort<TKey>(field, direction));
        return this;
    }

    /// <summary>
    /// Appends a descending sort clause.
    /// </summary>
    public QueryBuilder<TKey> OrderByDescending(TKey field)
        => OrderBy(field, SortDirection.Desc);

    /// <summary>
    /// Appends a secondary sort clause.
    /// </summary>
    public QueryBuilder<TKey> ThenBy(TKey field, SortDirection direction = SortDirection.Asc)
    {
        _sorts.Add(new Sort<TKey>(field, direction));
        return this;
    }

    /// <summary>
    /// Appends a secondary descending sort clause.
    /// </summary>
    public QueryBuilder<TKey> ThenByDescending(TKey field)
        => ThenBy(field, SortDirection.Desc);

    /// <summary>
    /// Sets the cursor for keyset pagination. Pass <c>null</c> to start from the beginning.
    /// </summary>
    public QueryBuilder<TKey> WithCursor(string? cursor)
    {
        _cursor = cursor;
        return this;
    }

    /// <summary>
    /// Sets the desired page size.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown immediately when <paramref name="pageSize"/> is not positive.</exception>
    public QueryBuilder<TKey> WithPageSize(int pageSize)
    {
        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than zero.");

        _pageSize = pageSize;
        return this;
    }

    /// <summary>
    /// Filter results to items whose spatial field intersects the given bounding box.
    /// Pass <c>null</c> to clear a previously set bounds.
    /// </summary>
    public QueryBuilder<TKey> WithSpatialBounds(BoundingBox? bounds)
    {
        _spatialBounds = bounds;
        return this;
    }

    internal ResolvedQuery<TKey> Build() => new()
    {
        Filters = _filters.AsReadOnly(),
        Sorts = _sorts.AsReadOnly(),
        Cursor = _cursor,
        PageSize = _pageSize,
        SpatialBounds = _spatialBounds,
    };
}