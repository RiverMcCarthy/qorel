using Qorel.Filters;

namespace Qorel.EntityFramework.Mapping;

/// <summary>
/// Implemented by <see cref="FieldMap{TEntity,TKey}"/> subclasses to identify the entity's
/// primary key field and expose filter application for custom repositories.
/// </summary>
/// <typeparam name="TEntity">The EF Core entity type.</typeparam>
/// <typeparam name="TKey">Enum type identifying fields in the schema.</typeparam>
public interface IFieldMap<TEntity, TKey>
    where TEntity : class
    where TKey : Enum
{
    /// <summary>
    /// The field key that maps to the entity's primary key.
    /// </summary>
    TKey IdKey { get; }

    /// <summary>
    /// The field key appended to every sort for deterministic cursor pagination.
    /// Must be unique per row. Defaults to <see cref="IdKey"/>.
    /// </summary>
    TKey TiebreakerKey { get; }

    /// <summary>
    /// Applies a list of typed filters to the query as a combined AND predicate.
    /// Use this to apply filters when building a custom <see cref="IQueryable{TEntity}"/>
    /// without going through the full pipeline.
    /// </summary>
    IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> query, IReadOnlyList<IFilter<TKey>> filters);
}
