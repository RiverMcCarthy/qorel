using Qorel.Filters;

namespace Qorel.Fields;

/// <summary>
/// Base typed field definition supporting equality and membership filter expressions.
/// </summary>
/// <typeparam name="TKey">Enum type identifying fields in the schema.</typeparam>
/// <typeparam name="TValue">The type of the field value.</typeparam>
public record Field<TKey, TValue>(TKey Key) : IField<TKey>
    where TKey : Enum
{
    /// <summary>
    /// Creates an equality filter for this field.
    /// </summary>
    public IFilter<TKey> Equals(TValue value)
        => new ScalarFilter<TKey, TValue>(FilterOperator.Equals) { Field = this, Value = value };

    /// <summary>
    /// Creates an <c>IsOneOf</c> membership filter for this field.
    /// </summary>
    public IFilter<TKey> IsOneOf(IReadOnlyList<TValue> values)
        => new CollectionFilter<TKey, TValue>(FilterOperator.IsOneOf) { Field = this, Values = values };

    /// <summary>
    /// Creates an <c>IsNotOneOf</c> exclusion filter for this field.
    /// </summary>
    public IFilter<TKey> IsNotOneOf(IReadOnlyList<TValue> values)
        => new CollectionFilter<TKey, TValue>(FilterOperator.IsNotOneOf) { Field = this, Values = values };

    /// <summary>
    /// Creates an <c>IsNull</c> filter for this field. Only valid for nullable fields.
    /// The schema will reject this operator for non-nullable value types.
    /// </summary>
    public IFilter<TKey> IsNull()
        => new NullCheckFilter<TKey, TValue>(FilterOperator.IsNull) { Field = this };

    /// <summary>
    /// Creates an <c>IsNotNull</c> filter for this field. Only valid for nullable fields.
    /// The schema will reject this operator for non-nullable value types.
    /// </summary>
    public IFilter<TKey> IsNotNull()
        => new NullCheckFilter<TKey, TValue>(FilterOperator.IsNotNull) { Field = this };
}
