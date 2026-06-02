using System.Text.Json.Serialization;

using Qorel.Serialization;

namespace Qorel;

/// <summary>
/// A raw sort specification received from a client request.
/// The pipeline validates each request against the schema before applying it.
/// </summary>
/// <typeparam name="TKey">Enum type identifying fields in the schema.</typeparam>
[JsonConverter(typeof(SortRequestJsonConverterFactory))]
public sealed record SortRequest<TKey> where TKey : Enum
{
    /// <summary>
    /// The key of the field to sort by.
    /// </summary>
    public required TKey FieldKey { get; init; }

    /// <summary>
    /// The sort direction. Defaults to <see cref="SortDirection.Asc"/>.
    /// </summary>
    public SortDirection Direction { get; init; }
}
