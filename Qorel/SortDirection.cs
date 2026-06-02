using System.Text.Json.Serialization;

namespace Qorel;

/// <summary>
/// The direction of a sort clause.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortDirection
{
    /// <summary>
    /// Ascending order.
    /// </summary>
    Asc,

    /// <summary>
    /// Descending order.
    /// </summary>
    Desc,
}
