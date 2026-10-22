using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qorel.Testing;

/// <summary>
/// Creates <see cref="FilterRequest{TField}"/> values from typed values, the way a client would send them, so tests
/// do not have to build <see cref="JsonElement"/> values by hand.
/// </summary>
public static class FilterRequestFactory
{
    // Enums go over the wire by name, so they are written the same way a real client would.
    private static readonly JsonSerializerOptions _jsonOptions = new() { Converters = { new JsonStringEnumConverter() } };

    /// <summary>
    /// Creates a filter with a single value, e.g. <c>Equals</c> or <c>GreaterThan</c>.
    /// </summary>
    public static FilterRequest<TField> Create<TField, TValue>(TField field, FilterOperator op, TValue value)
        where TField : struct, Enum
        => new() { Field = field, Operator = op, Value = JsonSerializer.SerializeToElement(value, _jsonOptions) };

    /// <summary>
    /// Creates a filter with a list of values, i.e. <c>IsOneOf</c> or <c>IsNotOneOf</c>.
    /// </summary>
    public static FilterRequest<TField> CreateList<TField, TValue>(TField field, FilterOperator op, IEnumerable<TValue> values)
        where TField : struct, Enum
        => new() { Field = field, Operator = op, Value = JsonSerializer.SerializeToElement(values, _jsonOptions) };

    /// <summary>
    /// Creates a filter with no value, i.e. <c>IsNull</c> or <c>IsNotNull</c>.
    /// </summary>
    public static FilterRequest<TField> CreateNullCheck<TField>(TField field, FilterOperator op)
        where TField : struct, Enum
        => new() { Field = field, Operator = op };
}
