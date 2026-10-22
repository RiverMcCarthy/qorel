namespace Qorel;

/// <summary>
/// A typed handle on one queryable field. Declare one per field as a static constant, then use it to map the field
/// in a <see cref="QueryModel{TEntity,TField}"/> and to build filters in code, e.g. <c>ProductFields.Id.EqualTo(id)</c>.
/// </summary>
/// <remarks>
/// Use <see cref="TextField{TField}"/> for strings and <see cref="ComparableField{TField,TValue}"/> for numbers and dates,
/// so their extra filters are available.
/// </remarks>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
/// <typeparam name="TValue">The field's value type.</typeparam>
/// <param name="key">The enum member that names this field.</param>
public class Field<TField, TValue>(TField key)
    where TField : struct, Enum
{
    // True for reference types and Nullable<T>, the types IsNull and IsNotNull make sense for.
    private static readonly bool _canBeNull = default(TValue) is null;

    /// <summary>
    /// The enum member that names this field.
    /// </summary>
    public TField Key { get; } = key;

    /// <summary>
    /// Matches items whose value equals <paramref name="value"/>.
    /// </summary>
    public Filter<TField> EqualTo(TValue value)
        => FieldFilter<TField, TValue>.Create(Key, FilterOperator.Equals, value);

    /// <summary>
    /// Matches items whose value is one of <paramref name="values"/>.
    /// </summary>
    public Filter<TField> IsOneOf(params TValue[] values)
        => IsOneOf((IEnumerable<TValue>)values);

    /// <inheritdoc cref="IsOneOf(TValue[])"/>
    public Filter<TField> IsOneOf(IEnumerable<TValue> values)
        => FieldFilter<TField, TValue>.CreateList(Key, FilterOperator.IsOneOf, values);

    /// <summary>
    /// Matches items whose value is none of <paramref name="values"/>.
    /// </summary>
    public Filter<TField> IsNotOneOf(params TValue[] values)
        => IsNotOneOf((IEnumerable<TValue>)values);

    /// <inheritdoc cref="IsNotOneOf(TValue[])"/>
    public Filter<TField> IsNotOneOf(IEnumerable<TValue> values)
        => FieldFilter<TField, TValue>.CreateList(Key, FilterOperator.IsNotOneOf, values);

    /// <summary>
    /// Matches items whose value is null.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TValue"/> cannot be null, e.g. <c>int</c>.</exception>
    public Filter<TField> IsNull() => NullCheck(FilterOperator.IsNull);

    /// <summary>
    /// Matches items whose value is not null.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TValue"/> cannot be null, e.g. <c>int</c>.</exception>
    public Filter<TField> IsNotNull() => NullCheck(FilterOperator.IsNotNull);

    /// <summary>
    /// Returns the field's name.
    /// </summary>
    public override string ToString() => Key.ToString();

    private FieldFilter<TField, TValue> NullCheck(FilterOperator op)
    {
        if (!_canBeNull)
            throw new InvalidOperationException($"'{Key}' is a {typeof(TValue).Name} field, which is never null.");

        return FieldFilter<TField, TValue>.CreateNullCheck(Key, op);
    }
}
