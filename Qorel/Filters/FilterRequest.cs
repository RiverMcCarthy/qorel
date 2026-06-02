using System.Text.Json;
using System.Text.Json.Serialization;

using Qorel.Schema;
using Qorel.Serialization;

namespace Qorel.Filters;

/// <summary>
/// A raw, unvalidated filter specification received from a client request.
/// The pipeline converts each request to a typed <see cref="IFilter{TKey}"/> after
/// validating it against the <see cref="FieldSchema{TKey}"/>.
/// </summary>
/// <typeparam name="TKey">Enum type identifying fields in the schema.</typeparam>
[JsonConverter(typeof(FilterRequestJsonConverterFactory))]
public sealed record FilterRequest<TKey> where TKey : Enum
{
    /// <summary>
    /// The field to apply the filter to.
    /// </summary>
    public required TKey FieldKey { get; init; }

    /// <summary>
    /// The filter operator to apply.
    /// </summary>
    public required FilterOperator Operator { get; init; }

    /// <summary>
    /// The raw JSON value.
    /// For <c>IsOneOf</c> and <c>IsNotOneOf</c> operators this must be a JSON array, otherwise a scalar value.
    /// </summary>
    public required JsonElement Value { get; init; }
}
