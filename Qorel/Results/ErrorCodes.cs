namespace Qorel.Results;

/// <summary>
/// Error codes returned in <see cref="ValidationError.Code"/>.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// The request references a field key not declared in the schema.
    /// </summary>
    public const string FieldInvalid = "field.invalid";

    /// <summary>
    /// The field exists in the schema but is not marked <c>Filterable()</c>.
    /// </summary>
    public const string FieldNotFilterable = "field.not_filterable";

    /// <summary>
    /// The field exists in the schema but is not marked <c>Sortable()</c>.
    /// </summary>
    public const string FieldNotSortable = "field.not_sortable";

    /// <summary>
    /// The operator is not in the field's allowed operator set.
    /// </summary>
    public const string OperatorNotAllowed = "operator.not_allowed";

    /// <summary>
    /// The pagination cursor is invalid e.g. malformed, expired, or has been tampered with.
    /// </summary>
    public const string CursorInvalid = "pagination.cursor_invalid";
}
