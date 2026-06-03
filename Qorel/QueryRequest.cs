using Qorel.Filters;
using Qorel.Spatial;

namespace Qorel;

/// <summary>
/// The request payload sent by a client to query a filtered, sorted, and paginated list.
/// Deserialise from the HTTP request body and pass directly to <c>QueryPipeline.QueryAsync</c>.
/// </summary>
/// <typeparam name="TKey">Enum type identifying fields in the schema.</typeparam>
public sealed record QueryRequest<TKey> where TKey : Enum
{
    /// <summary>
    /// Filter conditions to apply. Empty means no filtering will be applied.
    /// </summary>
    public List<FilterRequest<TKey>> Filters { get; init; } = [];

    /// <summary>
    /// Sort clauses applied in order. Empty means the default sort (ID ascending).
    /// </summary>
    public List<SortRequest<TKey>> Sorts { get; init; } = [];

    /// <summary>
    /// Cursor from the previous response. <c>null</c> for the first page.
    /// </summary>
    public string? Cursor { get; init; }

    /// <summary>
    /// Requested page size. Values below 1 use the configured default.
    /// Values above the configured maximum are clamped.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Optional geographic bounding box. When provided, only items whose spatial field intersects this box are returned.
    /// </summary>
    public BoundingBox? SpatialBounds { get; init; }
}