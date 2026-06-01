using System.Collections.Frozen;

using Qorel.Fields;
using Qorel.Filters;

namespace Qorel.Schema;

/// <summary>
/// Field metadata describing how a single field may be filtered and sorted by clients.
/// Produced by <see cref="FieldBuilder{TKey,TProp}"/> and registered in a <see cref="FieldSchema{TKey}"/>.
/// </summary>
/// <typeparam name="TKey">Enum type identifying fields in the schema.</typeparam>
public sealed record FieldMetadata<TKey>
{
    /// <summary>
    /// The typed field definition.
    /// </summary>
    public required IField<TKey> Field { get; init; }

    /// <summary>
    /// The CLR type of the field value.
    /// </summary>
    public required Type Type { get; init; }

    /// <summary>
    /// True if clients can include this field in filter expressions.
    /// </summary>
    public bool IsFilterable { get; init; }

    /// <summary>
    /// True if clients can include this field in sort clauses.
    /// </summary>
    public bool IsSortable { get; init; }

    /// <summary>
    /// The <see cref="FilterOperator"/> values allowed for this field.
    /// </summary>
    public required FrozenSet<FilterOperator> AllowedOperators { get; init; }
}
