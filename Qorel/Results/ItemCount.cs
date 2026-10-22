namespace Qorel;

/// <summary>
/// How many items match a query, from
/// <see cref="IQueryPipeline{TEntity,TField}.CountAsync(QueryRequest{TField}, IEnumerable{Filter{TField}}?, int?, CancellationToken)"/>.
/// </summary>
/// <param name="Count">The number of matching items, or the cap when <paramref name="IsCapped"/> is true.</param>
/// <param name="IsCapped">True when more items matched than the cap, e.g. so a UI can show "1000+".</param>
public sealed record ItemCount(int Count, bool IsCapped = false);
