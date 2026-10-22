namespace Qorel;

/// <summary>
/// The server filters registered for a model with <see cref="QorelBuilder.AddModel{TEntity,TField}"/>, worked out once per
/// request scope. Every pipeline call adds them, so a query cannot leave them out.
/// </summary>
internal sealed class ServerFilters<TEntity, TField>(IReadOnlyList<Filter<TField>> filters)
    where TEntity : class
    where TField : struct, Enum
{
    public IReadOnlyList<Filter<TField>> Filters { get; } = filters;

    /// <summary>
    /// The registered filters followed by <paramref name="extra"/>, the ones passed to a single call.
    /// </summary>
    public IReadOnlyList<Filter<TField>> With(IEnumerable<Filter<TField>>? extra) => [.. Filters, .. extra ?? []];
}
