using Qorel.Fields;

namespace Qorel.Filters;

internal abstract record Filter<TKey, TValue> : IFilter<TKey>
    where TKey : Enum
{
    public required Field<TKey, TValue> Field { get; init; }
    public TKey FieldKey => Field.Key;
    public abstract FilterOperator Operator { get; init; }
}

internal sealed record ScalarFilter<TKey, TValue>(FilterOperator Operator) : Filter<TKey, TValue>
    where TKey : Enum
{
    public required TValue Value { get; init; }
}

/// <summary>
/// Represents a 
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
/// <param name="Operator"></param>
internal sealed record CollectionFilter<TKey, TValue>(FilterOperator Operator) : Filter<TKey, TValue>
    where TKey : Enum
{
    public required IReadOnlyList<TValue> Values { get; init; }
}

/// <summary>
/// Represents an <c>IsNull</c> or <c>IsNotNull</c> filter.
/// No value is required, the filter is fully described by the operator and the field key.
/// </summary>
internal sealed record NullCheckFilter<TKey, TValue>(FilterOperator Operator) : Filter<TKey, TValue>
    where TKey : Enum;
