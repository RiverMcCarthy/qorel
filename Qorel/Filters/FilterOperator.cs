using System.Text.Json.Serialization;

namespace Qorel;

/// <summary>
/// How a filter compares a field with its value.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FilterOperator>))]
public enum FilterOperator
{
    /// <summary>
    /// The value equals the filter value.
    /// </summary>
    Equals,

    /// <summary>
    /// The text contains the filter value. Text fields only.
    /// </summary>
    Contains,

    /// <summary>
    /// The text starts with the filter value. Text fields only.
    /// </summary>
    StartsWith,

    /// <summary>
    /// The text ends with the filter value. Text fields only.
    /// </summary>
    EndsWith,

    /// <summary>
    /// The value is one of a list of values.
    /// </summary>
    IsOneOf,

    /// <summary>
    /// The value is none of a list of values.
    /// </summary>
    IsNotOneOf,

    /// <summary>
    /// The value is greater than the filter value. Ordered fields such as numbers and dates only.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// The value is greater than or equal to the filter value. Ordered fields only.
    /// </summary>
    GreaterThanOrEquals,

    /// <summary>
    /// The value is less than the filter value. Ordered fields only.
    /// </summary>
    LessThan,

    /// <summary>
    /// The value is less than or equal to the filter value. Ordered fields only.
    /// </summary>
    LessThanOrEquals,

    /// <summary>
    /// The value is null. Takes no filter value. Nullable fields only.
    /// </summary>
    IsNull,

    /// <summary>
    /// The value is not null. Takes no filter value. Nullable fields only.
    /// </summary>
    IsNotNull,
}
