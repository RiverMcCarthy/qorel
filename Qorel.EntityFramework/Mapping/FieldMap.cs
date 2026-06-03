using System.Collections.Frozen;
using System.Linq.Expressions;

using Qorel.EntityFramework.Extensions;
using Qorel.Filters;

namespace Qorel.EntityFramework.Mapping;

/// <summary>
/// Abstract base class that maps field keys to selector expressions over an EF Core entity.
/// Subclass once per entity, override <see cref="Build"/>, and register in DI.
/// </summary>
/// <typeparam name="TEntity">The EF Core entity type.</typeparam>
/// <typeparam name="TKey">Enum type identifying fields in the schema.</typeparam>
public abstract class FieldMap<TEntity, TKey> : IFieldMap<TEntity, TKey>
    where TEntity : class
    where TKey : Enum
{
    private readonly Lazy<FrozenDictionary<TKey, IFieldAccessor<TEntity>>> _accessors;

    /// <summary>
    /// Initialises the field map with lazy accessor construction.
    /// </summary>
    protected FieldMap()
    {
        _accessors = new Lazy<FrozenDictionary<TKey, IFieldAccessor<TEntity>>>(() =>
        {
            var builder = new FieldMapBuilder<TEntity, TKey>();
            Build(builder);
            return builder.Build();
        });
    }

    /// <inheritdoc/>
    public abstract TKey IdKey { get; }

    /// <inheritdoc/>
    public virtual TKey TiebreakerKey => IdKey;

    /// <inheritdoc/>
    public IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> query, IReadOnlyList<IFilter<TKey>> filters)
    {
        if (filters.Count == 0)
            return query;

        Expression<Func<TEntity, bool>> combined = filters
            .Select(f => _accessors.Value[f.FieldKey].BuildPredicate(f))
            .Aggregate((current, next) => current.And(next));

        return query.Where(combined);
    }

    /// <summary>
    /// Returns true if <paramref name="key"/> has a registered mapping.
    /// </summary>
    internal bool Has(TKey key) => _accessors.Value.ContainsKey(key);

    /// <summary>
    /// Returns the field accessor for <paramref name="key"/>.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the key is not registered.</exception>
    internal IFieldAccessor<TEntity> Get(TKey key)
    {
        if (!_accessors.Value.TryGetValue(key, out IFieldAccessor<TEntity>? accessor))
            throw new KeyNotFoundException($"Field '{key}' is not registered in {GetType().Name}.");

        return accessor;
    }

    /// <summary>
    /// Override to register all field-to-expression mappings via the <paramref name="map"/> builder.
    /// </summary>
    protected abstract void Build(FieldMapBuilder<TEntity, TKey> map);
}
