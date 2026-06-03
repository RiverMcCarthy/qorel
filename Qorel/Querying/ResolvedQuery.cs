using Qorel.Filters;
using Qorel.Spatial;

namespace Qorel.Querying;

/// <summary>
/// Resolved query parameters produced by converting a <see cref="QueryRequest{TKey}"/>
/// or building a <see cref="QueryBuilder{TKey}"/>.
/// </summary>
internal sealed record ResolvedQuery<TKey> where TKey : Enum
{
    public IReadOnlyList<IFilter<TKey>> Filters { get; init; } = [];
    public IReadOnlyList<ISort<TKey>> Sorts { get; init; } = [];
    public string? Cursor { get; init; }
    public int PageSize { get; init; }
    public BoundingBox? SpatialBounds { get; init; }
}