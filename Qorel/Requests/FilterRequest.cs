using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qorel;

/// <summary>
/// A filter as a client sends it, before it has been checked against a <see cref="QueryModel{TEntity,TField}"/>.
/// </summary>
/// <remarks>
/// In JSON: <c>{ "field": "Price", "operator": "LessThan", "value": 100 }</c>. Names are matched ignoring case and enums
/// are sent by name. <c>IsNull</c> and <c>IsNotNull</c> don't need a value.
/// </remarks>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
[JsonConverter(typeof(FilterRequestJsonConverter))]
public sealed record FilterRequest<TField>
    where TField : struct, Enum
{
    /// <summary>
    /// The field to filter.
    /// </summary>
    public required TField Field { get; init; }

    /// <summary>
    /// How to compare the field with <see cref="Value"/>.
    /// </summary>
    public required FilterOperator Operator { get; init; }

    /// <summary>
    /// The raw JSON value: an array for <c>IsOneOf</c> and <c>IsNotOneOf</c>, nothing for the null checks, and a single
    /// value otherwise. It is read as the field's type during validation.
    /// </summary>
    public JsonElement Value { get; init; }
}
