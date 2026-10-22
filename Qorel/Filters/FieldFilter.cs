using System.Diagnostics;

namespace Qorel;

/// <summary>
/// The one implementation of <see cref="Filter{TField}"/>. It knows the value type, so the field it targets can turn it
/// into a predicate without any casting of the values.
/// </summary>
internal sealed class FieldFilter<TField, TValue> : Filter<TField>
    where TField : struct, Enum
{
    private FieldFilter(TField field, FilterOperator op, TValue value, IReadOnlyList<TValue> values)
        : base(field, op)
    {
        Value = value;
        Values = values;
    }

    /// <summary>
    /// The value for single-value operators such as <c>Equals</c> and <c>LessThan</c>. Default otherwise.
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// The values for <c>IsOneOf</c> and <c>IsNotOneOf</c>. Empty otherwise.
    /// </summary>
    public IReadOnlyList<TValue> Values { get; }

    internal override Type ValueType => typeof(TValue);

    public static FieldFilter<TField, TValue> Create(TField field, FilterOperator op, TValue value)
    {
        Debug.Assert(op.GetOperand() == Operand.Single, $"{op} does not take a single value.");
        return new(field, op, value, []);
    }

    public static FieldFilter<TField, TValue> CreateList(TField field, FilterOperator op, IEnumerable<TValue> values)
    {
        Debug.Assert(op.GetOperand() == Operand.List, $"{op} does not take a list of values.");
        ArgumentNullException.ThrowIfNull(values);
        return new(field, op, default!, [.. values]);
    }

    public static FieldFilter<TField, TValue> CreateNullCheck(TField field, FilterOperator op)
    {
        Debug.Assert(op.GetOperand() == Operand.None, $"{op} takes a value.");
        return new(field, op, default!, []);
    }

    private protected override string FormatOperand() => Operator.GetOperand() switch
    {
        Operand.None => string.Empty,
        Operand.Single => $"={FilterValueFormatter.Format(Value)}",

        // Sorted and de-duplicated, since IsOneOf [a, b] matches the same items as [b, a, a].
        Operand.List => $"=[{string.Join(",", Values.Select(v => FilterValueFormatter.Format(v)).Distinct().Order(StringComparer.Ordinal))}]",

        _ => throw new UnreachableException(),
    };
}
