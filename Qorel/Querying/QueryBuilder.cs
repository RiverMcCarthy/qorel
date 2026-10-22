namespace Qorel;

/// <summary>
/// Builds a query in code, from filters you have created with the field handles.
/// </summary>
/// <remarks>
/// Use it where the query comes from your own code rather than a client: a background job, an internal service, a test.
/// Nothing is checked against the model's client rules, so any mapped field can be filtered or sorted.
/// </remarks>
/// <example>
/// <code>
/// var query = new QueryBuilder&lt;ProductField&gt;()
///     .Where(ProductFields.Price.LessThan(100m))
///     .OrderByDescending(ProductFields.CreatedAt)
///     .WithPageSize(50);
/// </code>
/// </example>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
public sealed class QueryBuilder<TField>
    where TField : struct, Enum
{
    private readonly List<Filter<TField>> _filters = [];
    private readonly List<Sort<TField>> _sorts = [];
    private string? _search;
    private string? _cursor;
    private int? _pageSize;
    private BoundingBox? _spatialBounds;

    /// <summary>
    /// Adds filters. An item has to match every filter.
    /// </summary>
    public QueryBuilder<TField> Where(params Filter<TField>[] filters)
        => Where((IEnumerable<Filter<TField>>)filters);

    /// <inheritdoc cref="Where(Filter{TField}[])"/>
    public QueryBuilder<TField> Where(IEnumerable<Filter<TField>> filters)
    {
        ArgumentNullException.ThrowIfNull(filters);

        foreach (Filter<TField> filter in filters)
        {
            ArgumentNullException.ThrowIfNull(filter, nameof(filters));
            _filters.Add(filter);
        }

        return this;
    }

    /// <summary>
    /// Sorts by <paramref name="field"/>, smallest first, replacing any sorts added before.
    /// </summary>
    public QueryBuilder<TField> OrderBy<TValue>(Field<TField, TValue> field)
        where TValue : IComparable<TValue>
        => Sort(field?.Key, SortDirection.Asc, replace: true);

    /// <inheritdoc cref="OrderBy{TValue}(Field{TField, TValue})"/>
    /// <remarks>Nulls sort last.</remarks>
    public QueryBuilder<TField> OrderBy<TValue>(Field<TField, TValue?> field)
        where TValue : struct, IComparable<TValue>
        => Sort(field?.Key, SortDirection.Asc, replace: true);

    /// <summary>
    /// Sorts by <paramref name="field"/>, largest first, replacing any sorts added before.
    /// </summary>
    public QueryBuilder<TField> OrderByDescending<TValue>(Field<TField, TValue> field)
        where TValue : IComparable<TValue>
        => Sort(field?.Key, SortDirection.Desc, replace: true);

    /// <inheritdoc cref="OrderByDescending{TValue}(Field{TField, TValue})"/>
    /// <remarks>Nulls sort first.</remarks>
    public QueryBuilder<TField> OrderByDescending<TValue>(Field<TField, TValue?> field)
        where TValue : struct, IComparable<TValue>
        => Sort(field?.Key, SortDirection.Desc, replace: true);

    /// <summary>
    /// Adds a sort by <paramref name="field"/>, smallest first, for items the earlier sorts consider equal.
    /// </summary>
    public QueryBuilder<TField> ThenBy<TValue>(Field<TField, TValue> field)
        where TValue : IComparable<TValue>
        => Sort(field?.Key, SortDirection.Asc, replace: false);

    /// <inheritdoc cref="ThenBy{TValue}(Field{TField, TValue})"/>
    /// <remarks>Nulls sort last.</remarks>
    public QueryBuilder<TField> ThenBy<TValue>(Field<TField, TValue?> field)
        where TValue : struct, IComparable<TValue>
        => Sort(field?.Key, SortDirection.Asc, replace: false);

    /// <summary>
    /// Adds a sort by <paramref name="field"/>, largest first, for items the earlier sorts consider equal.
    /// </summary>
    public QueryBuilder<TField> ThenByDescending<TValue>(Field<TField, TValue> field)
        where TValue : IComparable<TValue>
        => Sort(field?.Key, SortDirection.Desc, replace: false);

    /// <inheritdoc cref="ThenByDescending{TValue}(Field{TField, TValue})"/>
    /// <remarks>Nulls sort first.</remarks>
    public QueryBuilder<TField> ThenByDescending<TValue>(Field<TField, TValue?> field)
        where TValue : struct, IComparable<TValue>
        => Sort(field?.Key, SortDirection.Desc, replace: false);

    /// <summary>
    /// Searches the model's searchable fields for <paramref name="text"/>. Every word has to appear in at least one of
    /// them. <c>null</c> removes the search.
    /// </summary>
    public QueryBuilder<TField> WithSearch(string? text)
    {
        _search = text;
        return this;
    }

    /// <summary>
    /// Starts after <paramref name="cursor"/>, the <see cref="Page{T}.NextCursor"/> of the previous page.
    /// <c>null</c> starts from the first page.
    /// </summary>
    public QueryBuilder<TField> WithCursor(string? cursor)
    {
        _cursor = cursor;
        return this;
    }

    /// <summary>
    /// Sets how many items a page holds. Without it, <see cref="QorelOptions.DefaultPageSize"/> is used.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pageSize"/> is not positive.</exception>
    public QueryBuilder<TField> WithPageSize(int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        _pageSize = pageSize;
        return this;
    }

    /// <summary>
    /// Only returns items inside <paramref name="bounds"/>. <c>null</c> removes the bounds.
    /// </summary>
    public QueryBuilder<TField> WithSpatialBounds(BoundingBox? bounds)
    {
        _spatialBounds = bounds;
        return this;
    }

    internal IReadOnlyList<Filter<TField>> Filters => _filters;

    internal IReadOnlyList<Sort<TField>> Sorts => _sorts;

    internal string? Search => _search;

    internal string? Cursor => _cursor;

    internal int? PageSize => _pageSize;

    internal BoundingBox? SpatialBounds => _spatialBounds;

    // Every sort method ends up here. Their constraints have already limited them to ordered types.
    // The field is the handle's key, or null when the caller passed a null handle.
    private QueryBuilder<TField> Sort(TField? field, SortDirection direction, bool replace)
    {
        if (field is not { } key)
            throw new ArgumentNullException(nameof(field));

        if (replace)
            _sorts.Clear();

        if (_sorts.Exists(sort => EqualityComparer<TField>.Default.Equals(sort.Field, key)))
            throw new ArgumentException($"The query is already sorted by '{key}'.", nameof(field));

        _sorts.Add(new Sort<TField>(key, direction));
        return this;
    }
}
