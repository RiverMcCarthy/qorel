namespace Qorel;

/// <summary>
/// A handle on an ordered field such as a number or a date. Adds range filters to the ones on
/// <see cref="Field{TField,TValue}"/>.
/// </summary>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
/// <typeparam name="TValue">The field's value type.</typeparam>
/// <param name="key">The enum member that names this field.</param>
public sealed class ComparableField<TField, TValue>(TField key) : Field<TField, TValue>(key)
    where TField : struct, Enum
    where TValue : IComparable<TValue>
{
    /// <summary>
    /// Matches items whose value is greater than <paramref name="value"/>.
    /// </summary>
    public Filter<TField> GreaterThan(TValue value)
        => FieldFilter<TField, TValue>.Create(Key, FilterOperator.GreaterThan, value);

    /// <summary>
    /// Matches items whose value is greater than or equal to <paramref name="value"/>.
    /// </summary>
    public Filter<TField> GreaterThanOrEquals(TValue value)
        => FieldFilter<TField, TValue>.Create(Key, FilterOperator.GreaterThanOrEquals, value);

    /// <summary>
    /// Matches items whose value is less than <paramref name="value"/>.
    /// </summary>
    public Filter<TField> LessThan(TValue value)
        => FieldFilter<TField, TValue>.Create(Key, FilterOperator.LessThan, value);

    /// <summary>
    /// Matches items whose value is less than or equal to <paramref name="value"/>.
    /// </summary>
    public Filter<TField> LessThanOrEquals(TValue value)
        => FieldFilter<TField, TValue>.Create(Key, FilterOperator.LessThanOrEquals, value);
}
