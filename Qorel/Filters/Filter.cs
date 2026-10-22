namespace Qorel;

/// <summary>
/// A condition on one field, e.g. "Price is less than 100".
/// </summary>
/// <remarks>
/// Get filters from the field handles, e.g. <c>ProductFields.Price.LessThan(100m)</c>, or from client input with
/// <see cref="QueryModel{TEntity,TField}.ParseFilters(IEnumerable{FilterRequest{TField}})"/>.
/// Two filters are equal when they have the same <see cref="CacheKey"/>.
/// </remarks>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
public abstract class Filter<TField> : IEquatable<Filter<TField>>
    where TField : struct, Enum
{
    private string? _cacheKey;

    // Only FieldFilter derives from this, so every filter is one Qorel knows how to run.
    private protected Filter(TField field, FilterOperator @operator)
    {
        Field = field;
        Operator = @operator;
    }

    /// <summary>
    /// The field being filtered.
    /// </summary>
    public TField Field { get; }

    /// <summary>
    /// How the field is compared.
    /// </summary>
    public FilterOperator Operator { get; }

    /// <summary>
    /// The filter written out the same way every time, whatever the culture, e.g. <c>Price.LessThan=100</c>.
    /// Values that mean the same thing give the same key, such as <c>100</c> and <c>100.0</c>.
    /// </summary>
    public string CacheKey => _cacheKey ??= $"{Field}.{Operator}{FormatOperand()}";

    /// <summary>
    /// The type of the value the filter compares against.
    /// </summary>
    internal abstract Type ValueType { get; }

    /// <summary>
    /// Returns true when both filters have the same <see cref="CacheKey"/>.
    /// </summary>
    public bool Equals(Filter<TField>? other)
        => other is not null && string.Equals(CacheKey, other.CacheKey, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Filter<TField>);

    /// <inheritdoc/>
    public override int GetHashCode() => CacheKey.GetHashCode(StringComparison.Ordinal);

    /// <summary>
    /// Returns <see cref="CacheKey"/>.
    /// </summary>
    public override string ToString() => CacheKey;

    /// <summary>
    /// The value part of <see cref="CacheKey"/>, e.g. <c>=100</c>, or empty when the operator takes no value.
    /// </summary>
    private protected abstract string FormatOperand();
}
