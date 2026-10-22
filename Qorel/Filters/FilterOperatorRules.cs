using System.Collections.Frozen;

namespace Qorel;

/// <summary>
/// Which operators each field type supports, and what each operator takes as its value.
/// </summary>
internal static class FilterOperatorRules
{
    private static readonly FrozenSet<FilterOperator> _equality = FilterOperators.Equality.ToFrozenSet();
    private static readonly FrozenSet<FilterOperator> _ordered = FilterOperators.Equality.Union(FilterOperators.Comparison).ToFrozenSet();
    private static readonly FrozenSet<FilterOperator> _text =
        FilterOperators.Equality.Union(FilterOperators.Text).Union(FilterOperators.NullCheck).ToFrozenSet();

    private static readonly FrozenDictionary<Type, FrozenSet<FilterOperator>> _byType = new Dictionary<Type, FrozenSet<FilterOperator>>
    {
        [typeof(string)] = _text,
        [typeof(bool)] = _equality,
        [typeof(Guid)] = _equality,
        [typeof(byte)] = _ordered,
        [typeof(short)] = _ordered,
        [typeof(int)] = _ordered,
        [typeof(long)] = _ordered,
        [typeof(float)] = _ordered,
        [typeof(double)] = _ordered,
        [typeof(decimal)] = _ordered,
        [typeof(DateTime)] = _ordered,
        [typeof(DateTimeOffset)] = _ordered,
        [typeof(DateOnly)] = _ordered,
        [typeof(TimeOnly)] = _ordered,
        [typeof(TimeSpan)] = _ordered,
    }.ToFrozenDictionary();

    public static Operand GetOperand(this FilterOperator op) => op switch
    {
        FilterOperator.IsNull or FilterOperator.IsNotNull => Operand.None,
        FilterOperator.IsOneOf or FilterOperator.IsNotOneOf => Operand.List,
        _ => Operand.Single,
    };

    /// <summary>
    /// Returns every operator a field of type <paramref name="type"/> supports. Nullable value types also get the
    /// null checks, and enums get the equality operators.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="type"/> cannot be filtered.</exception>
    public static FrozenSet<FilterOperator> SupportedBy(Type type)
    {
        Type? underlying = Nullable.GetUnderlyingType(type);
        Type valueType = underlying ?? type;

        FrozenSet<FilterOperator> operators = valueType.IsEnum
            ? _equality
            : _byType.GetValueOrDefault(valueType)
                ?? throw new NotSupportedException(
                    $"'{type.Name}' fields cannot be filtered. Supported types are string, bool, Guid, enums, " +
                    "numbers (byte, short, int, long, float, double, decimal), DateTime, DateTimeOffset, DateOnly, TimeOnly and TimeSpan.");

        return underlying is null ? operators : operators.Union(FilterOperators.NullCheck).ToFrozenSet();
    }
}
