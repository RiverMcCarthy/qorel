using System.Text.Json.Serialization;

namespace Qorel;

/// <summary>
/// The direction of a sort.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SortDirection>))]
public enum SortDirection
{
    /// <summary>
    /// Smallest first. Nulls come last.
    /// </summary>
    Asc,

    /// <summary>
    /// Largest first. Nulls come first.
    /// </summary>
    Desc,
}
