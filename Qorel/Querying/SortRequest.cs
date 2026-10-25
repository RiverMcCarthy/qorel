using System.Text.Json.Serialization;

namespace Qorel;

/// <summary>
/// A sort as a client sends it, before it has been checked against a <see cref="QueryModel{TEntity,TField}"/>.
/// </summary>
/// <remarks>
/// In JSON: <c>{ "field": "Price", "direction": "Desc" }</c>. The direction defaults to <c>Asc</c>.
/// </remarks>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
[JsonConverter(typeof(SortRequestJsonConverter))]
public sealed record SortRequest<TField>
    where TField : struct, Enum
{
    /// <summary>
    /// The field to sort by.
    /// </summary>
    public required TField Field { get; init; }

    /// <summary>
    /// The sort direction. Defaults to <see cref="SortDirection.Asc"/>.
    /// </summary>
    public SortDirection Direction { get; init; }
}
