using System.Collections.Frozen;

using Qorel.Fields;
using Qorel.Filters;

namespace Qorel.Schema;

/// <summary>
/// Fluent builder for declaring <see cref="FieldMetadata{TKey}"/> in a <see cref="FieldSchema{TKey}"/>.
/// Instance is returned via <see cref="FieldSchema{TKey}.For{TProp}"/>.
/// </summary>
/// <remarks>
/// <c>Sortable()</c> is available as an extension method in <see cref="FieldBuilderExtensions"/> to enforce the IComparable constraint
/// </remarks>
/// <typeparam name="TKey">Enum type identifying fields in the schema.</typeparam>
/// <typeparam name="TProp">The CLR type of the field value.</typeparam>
public sealed class FieldBuilder<TKey, TProp>(Field<TKey, TProp> field)
    where TKey : Enum
{
    private bool _filterable;
    private bool _sortable;
    private FrozenSet<FilterOperator> _operators = FrozenSet<FilterOperator>.Empty;

    /// <summary>
    /// Marks this field as filterable.
    /// When <paramref name="operators"/> is omitted, the allowed set is inferred from the field type
    /// </summary>
    public FieldBuilder<TKey, TProp> Filterable(IReadOnlyList<FilterOperator>? operators = null)
    {
        _filterable = true;
        _operators = operators?.ToFrozenSet() ?? FilterTypeRules.GetValidOperators(typeof(TProp)).ToFrozenSet();
        return this;
    }

    /// <summary>
    /// Produces the completed <see cref="FieldMetadata{TKey}"/>.
    /// </summary>
    public FieldMetadata<TKey> Build() => new()
    {
        Field = field,
        Type = typeof(TProp),
        IsFilterable = _filterable,
        IsSortable = _sortable,
        AllowedOperators = _operators,
    };

    internal void MarkSortable() => _sortable = true;

    /// <summary>
    /// Implicitly converts the builder to <see cref="FieldMetadata{TKey}"/> so it can be
    /// returned directly from <see cref="FieldSchema{TKey}.Define"/> without calling <see cref="Build"/>.
    /// </summary>
    public static implicit operator FieldMetadata<TKey>(FieldBuilder<TKey, TProp> builder)
        => builder.Build();
}
