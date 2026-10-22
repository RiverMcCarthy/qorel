namespace Qorel;

/// <summary>
/// The codes used in <see cref="ValidationError.Code"/>. They are stable, so clients can match on them.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// The request names a field that does not exist, or that is only for server-side use.
    /// </summary>
    /// <remarks>The value is <c>field.invalid</c>, kept from 1.x so existing clients still match it.</remarks>
    public const string FieldUnknown = "field.invalid";

    /// <summary>
    /// The field exists but is not marked <c>Filterable()</c>.
    /// </summary>
    public const string FieldNotFilterable = "field.not_filterable";

    /// <summary>
    /// The field exists but is not marked <c>Sortable()</c>.
    /// </summary>
    public const string FieldNotSortable = "field.not_sortable";

    /// <summary>
    /// The field does not allow the operator.
    /// </summary>
    public const string OperatorNotAllowed = "operator.not_allowed";

    /// <summary>
    /// The filter value cannot be read as the field's type, e.g. a string for a number, an unknown enum name,
    /// or a single value where <c>IsOneOf</c> expects an array.
    /// </summary>
    public const string FilterValueInvalid = "filter.value_invalid";

    /// <summary>
    /// An <c>IsOneOf</c> or <c>IsNotOneOf</c> filter has more values than <see cref="QorelOptions.MaxFilterValues"/>.
    /// </summary>
    public const string FilterTooManyValues = "filter.too_many_values";

    /// <summary>
    /// The request has more filters than <see cref="QorelOptions.MaxFilters"/>.
    /// </summary>
    public const string FiltersTooMany = "filter.too_many";

    /// <summary>
    /// The filters could not be read, e.g. broken JSON, something other than an array, or a null entry.
    /// </summary>
    public const string FiltersMalformed = "filter.malformed";

    /// <summary>
    /// The request has more sorts than <see cref="QorelOptions.MaxSorts"/>.
    /// </summary>
    public const string SortsTooMany = "sort.too_many";

    /// <summary>
    /// A sort could not be read, e.g. a null entry.
    /// </summary>
    public const string SortsMalformed = "sort.malformed";

    /// <summary>
    /// The same field appears in the sorts more than once.
    /// </summary>
    public const string SortDuplicate = "sort.duplicate_field";

    /// <summary>
    /// The request has search text, but the model has no searchable fields.
    /// </summary>
    public const string SearchNotSupported = "search.not_supported";

    /// <summary>
    /// The search text is longer than <see cref="QorelOptions.MaxSearchLength"/>.
    /// </summary>
    public const string SearchTooLong = "search.too_long";

    /// <summary>
    /// The cursor cannot be read, has been changed, or was created for different sorts.
    /// </summary>
    public const string CursorInvalid = "pagination.cursor_invalid";

    /// <summary>
    /// The bounding box has a coordinate out of range, or a minimum greater than its maximum.
    /// </summary>
    public const string SpatialBoundsInvalid = "spatial.bounds_invalid";

    /// <summary>
    /// The request has spatial bounds, but the model has no spatial filter.
    /// </summary>
    public const string SpatialNotSupported = "spatial.not_supported";
}
