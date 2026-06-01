using Qorel.Filters;

namespace Qorel.Results;

/// <summary>
/// A single validation error produced during request conversion or schema validation.
/// </summary>
/// <param name="Code">
/// Error code (e.g. <c>"field.invalid"</c>).
/// </param>
/// <param name="Message">Description of the error.</param>
/// <param name="Field">The field name that caused the error, or <c>null</c> for request-level errors.</param>
public sealed record ValidationError(string Code, string Message, string? Field = null)
{
    internal static ValidationError InvalidField<TKey>(TKey field) where TKey : Enum
        => new(ErrorCodes.FieldInvalid, $"Field '{field}' is not recognised.", field!.ToString());

    internal static ValidationError FieldNotFilterable<TKey>(TKey field) where TKey : Enum
        => new(ErrorCodes.FieldNotFilterable, $"Field '{field}' cannot be used in a filter.", field!.ToString());

    internal static ValidationError FieldNotSortable<TKey>(TKey field) where TKey : Enum
        => new(ErrorCodes.FieldNotSortable, $"Field '{field}' cannot be used in a sort.", field!.ToString());

    internal static ValidationError OperatorNotAllowed<TKey>(TKey field, FilterOperator op) where TKey : Enum
        => new(ErrorCodes.OperatorNotAllowed, $"Operator '{op}' is not allowed for field '{field}'.", field!.ToString());

    internal static ValidationError CursorInvalid()
        => new(ErrorCodes.CursorInvalid, "The pagination cursor is invalid or has expired.");
}
