using System.Text.Json.Serialization;

namespace Qorel.Filters;

/// <summary>
/// The comparison operator applied when filtering a field.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FilterOperator
{
    /// <summary>
    /// Exact equality match.
    /// </summary>
    Equals,

    /// <summary>
    /// Substring match — case sensitivity depends on the database.
    /// Valid for <see cref="string"/> fields only.
    /// </summary>
    Contains,

    /// <summary>
    /// Prefix match. Valid for <see cref="string"/> fields only.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Suffix match. Valid for <see cref="string"/> fields only.
    /// </summary>
    EndsWith,

    /// <summary>
    /// Field value is in the provided collection.
    /// </summary>
    IsOneOf,

    /// <summary>
    /// Field value is not in the provided collection.
    /// </summary>
    IsNotOneOf,

    /// <summary>
    /// Strict greater-than. Valid for numeric and date fields.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Greater-than-or-equal. Valid for numeric and date fields.
    /// </summary>
    GreaterThanOrEquals,

    /// <summary>
    /// Strict less-than. Valid for numeric and date fields.
    /// </summary>
    LessThan,

    /// <summary>
    /// Less-than-or-equal. Valid for numeric and date fields.
    /// </summary>
    LessThanOrEquals,

    /// <summary>
    /// Field value is null. Valid for nullable fields only.
    /// </summary>
    IsNull,

    /// <summary>
    /// Field value is not null. Valid for nullable fields only.
    /// </summary>
    IsNotNull,
}
