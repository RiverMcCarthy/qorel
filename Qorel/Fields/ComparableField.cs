using Qorel.Filters;

namespace Qorel.Fields;

/// <summary>
/// A typed field definition for comparable value types, extending <see cref="Field{TKey,TValue}"/> with range filter expressions.
/// </summary>
/// <typeparam name="TKey">Enum type identifying fields in the schema.</typeparam>
/// <typeparam name="TValue">The CLR type of the field value - must implement <see cref="IComparable{T}"/>.</typeparam>
public sealed record ComparableField<TKey, TValue>(TKey Key) : Field<TKey, TValue>(Key)
    where TKey : Enum
    where TValue : IComparable<TValue>
{
    /// <summary>
    /// Creates a <c>GreaterThan</c> filter for this field.
    /// </summary>
    public IFilter<TKey> GreaterThan(TValue value)
        => new ScalarFilter<TKey, TValue>(FilterOperator.GreaterThan) { Field = this, Value = value };

    /// <summary>
    /// Creates a <c>GreaterThanOrEquals</c> filter for this field.
    /// </summary>
    public IFilter<TKey> GreaterThanOrEquals(TValue value)
        => new ScalarFilter<TKey, TValue>(FilterOperator.GreaterThanOrEquals) { Field = this, Value = value };

    /// <summary>
    /// Creates a <c>LessThan</c> filter for this field.
    /// </summary>
    public IFilter<TKey> LessThan(TValue value)
        => new ScalarFilter<TKey, TValue>(FilterOperator.LessThan) { Field = this, Value = value };

    /// <summary>
    /// Creates a <c>LessThanOrEquals</c> filter for this field.
    /// </summary>
    public IFilter<TKey> LessThanOrEquals(TValue value)
        => new ScalarFilter<TKey, TValue>(FilterOperator.LessThanOrEquals) { Field = this, Value = value };
}
