using System.Collections.Frozen;
using System.Linq.Expressions;

namespace Qorel;

/// <summary>
/// Everything a <see cref="QueryModel{TEntity,TField}"/> declared in <c>Configure</c>, plus the options it runs with.
/// Built once and never changed.
/// </summary>
internal sealed class BuiltModel<TEntity, TField>
    where TEntity : class
    where TField : struct, Enum
{
    public required string Name { get; init; }

    public required QorelOptions Options { get; init; }

    public required FrozenDictionary<TField, MappedField<TEntity, TField>> Fields { get; init; }

    public required MappedField<TEntity, TField> Id { get; init; }

    /// <summary>
    /// The sorts used when a query does not ask for any. Empty means sort by id.
    /// </summary>
    public required IReadOnlyList<Sort<TField>> DefaultSorts { get; init; }

    /// <summary>
    /// The fields a request's search text is matched against.
    /// </summary>
    public required IReadOnlyList<MappedField<TEntity, TField>> SearchFields { get; init; }

    public required IReadOnlyList<ClientField<TField>> ClientFields { get; init; }

    public required Func<BoundingBox, Expression<Func<TEntity, bool>>>? SpatialFilter { get; init; }

    /// <summary>
    /// Finds a field a client named. Fields that are only for server-side use are not found.
    /// </summary>
    public MappedField<TEntity, TField>? FindClientField(TField field)
        => Fields.TryGetValue(field, out MappedField<TEntity, TField>? mapped) && mapped.Client.IsVisible ? mapped : null;

    /// <summary>
    /// Gets a field that code refers to, e.g. in a filter built with a field handle.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the field is not mapped, which is a bug in the calling code.</exception>
    public MappedField<TEntity, TField> GetField(TField field)
        => Fields.TryGetValue(field, out MappedField<TEntity, TField>? mapped)
            ? mapped
            : throw new InvalidOperationException($"'{field}' is not mapped in {Name}. Map it in Configure to filter or sort by it.");

    /// <exception cref="InvalidOperationException">Thrown when a filter's field is not mapped.</exception>
    public IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> source, IEnumerable<Filter<TField>> filters)
    {
        foreach (Filter<TField> filter in filters)
            source = source.Where(GetField(filter.Field).ToExpression(filter));

        return source;
    }

    /// <exception cref="InvalidOperationException">Thrown when a filter's field is not mapped.</exception>
    public IEnumerable<TEntity> ApplyFilters(IEnumerable<TEntity> source, IEnumerable<Filter<TField>> filters)
    {
        Func<TEntity, bool>[] predicates = [.. filters.Select(filter => GetField(filter.Field).ToDelegate(filter))];

        return predicates.Length == 0 ? source : source.Where(entity => MatchesAll(entity, predicates));
    }

    // A plain loop, since this runs once per item and a lambda here would allocate each time.
    private static bool MatchesAll(TEntity entity, Func<TEntity, bool>[] predicates)
    {
        foreach (Func<TEntity, bool> predicate in predicates)
        {
            if (!predicate(entity))
                return false;
        }

        return true;
    }
}
