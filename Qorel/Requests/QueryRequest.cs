namespace Qorel;

/// <summary>
/// A query as a client sends it in a request body: filters, sorts, search text, a page size, and a cursor. Pass it
/// to <see cref="IQueryPipeline{TEntity,TField}.GetPageAsync{TResult}(QueryRequest{TField}, System.Linq.Expressions.Expression{Func{TEntity, TResult}}, IEnumerable{Filter{TField}}?, CancellationToken)"/>,
/// which checks it against the query model before running it. For a GET endpoint, use <c>QueryParameters</c> from
/// <c>Qorel.AspNetCore</c>.
/// </summary>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
public sealed record QueryRequest<TField>
    where TField : struct, Enum
{
    /// <summary>
    /// Filters an item must match all of. Empty means no filtering.
    /// </summary>
    public IReadOnlyList<FilterRequest<TField>> Filters { get; init; } = [];

    /// <summary>
    /// Sorts, applied in order. Empty uses the model's default sort, or the id.
    /// </summary>
    public IReadOnlyList<SortRequest<TField>> Sorts { get; init; } = [];

    /// <summary>
    /// Text to search for. Every word has to appear in at least one of the model's searchable fields.
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// The <see cref="Page{T}.NextCursor"/> from the previous page, or <c>null</c> for the first page. A cursor only
    /// works with the sorts it was created with.
    /// </summary>
    public string? Cursor { get; init; }

    /// <summary>
    /// How many items to return. <c>null</c>, zero, or a negative number uses the default page size, and anything above
    /// the maximum is lowered to it. Both come from <see cref="QorelOptions"/>, unless the model sets its own.
    /// </summary>
    public int? PageSize { get; init; }

    /// <summary>
    /// Only return items inside this box. The model needs a spatial filter for this to work.
    /// </summary>
    public BoundingBox? SpatialBounds { get; init; }
}
