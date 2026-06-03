using System.Collections.Frozen;
using System.Linq.Expressions;

using Qorel.Fields;

namespace Qorel.EntityFramework.Mapping;

/// <summary>
/// Fluent builder for registering field mappings inside <see cref="Build"/>.
/// </summary>
public sealed class FieldMapBuilder<TEntity, TKey>
    where TEntity : class
    where TKey : Enum
{
    private readonly Dictionary<TKey, IFieldAccessor<TEntity>> _entries = [];

    /// <summary>
    /// Maps a <see cref="Field{TKey,TValue}"/> to the LINQ selector expression
    /// that extracts the field value from an entity.
    /// </summary>
    public FieldMapBuilder<TEntity, TKey> Add<TValue>(Field<TKey, TValue> field, Expression<Func<TEntity, TValue>> selector)
    {
        if (_entries.ContainsKey(field.Key))
            throw new InvalidOperationException($"Field '{field.Key}' is already registered. Each field key may only be mapped once.");

        _entries[field.Key] = new FieldAccessor<TEntity, TValue>(selector);
        return this;
    }

    internal FrozenDictionary<TKey, IFieldAccessor<TEntity>> Build()
        => _entries.ToFrozenDictionary();
}