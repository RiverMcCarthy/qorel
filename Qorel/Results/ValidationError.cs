namespace Qorel;

/// <summary>
/// One thing wrong with a client's query.
/// </summary>
/// <param name="Code">A stable, machine-readable code from <see cref="ErrorCodes"/>, e.g. <c>field.invalid</c>.</param>
/// <param name="Message">A description for developers. It may change between versions, so match on <paramref name="Code"/>.</param>
/// <param name="Field">The field the error is about, or <c>null</c> for errors about the whole request.</param>
public sealed record ValidationError(string Code, string Message, string? Field = null)
{
    internal static ValidationError UnknownField<TField>(TField field) where TField : struct, Enum
        => UnknownField(field.ToString());

    internal static ValidationError UnknownField(string field)
        => new(ErrorCodes.FieldUnknown, $"Field '{field}' is not recognised.", field);

    internal static ValidationError FieldNotFilterable<TField>(TField field) where TField : struct, Enum
        => new(ErrorCodes.FieldNotFilterable, $"Field '{field}' cannot be used in a filter.", field.ToString());

    internal static ValidationError FieldNotSortable<TField>(TField field) where TField : struct, Enum
        => new(ErrorCodes.FieldNotSortable, $"Field '{field}' cannot be used in a sort.", field.ToString());

    internal static ValidationError OperatorNotAllowed<TField>(TField field, FilterOperator op) where TField : struct, Enum
        => new(ErrorCodes.OperatorNotAllowed, $"Operator '{op}' is not allowed for field '{field}'.", field.ToString());

    internal static ValidationError FilterValueInvalid<TField>(TField field, FilterOperator op) where TField : struct, Enum
        => new(ErrorCodes.FilterValueInvalid, $"The value for field '{field}' is not valid for operator '{op}'.", field.ToString());

    internal static ValidationError TooManyFilterValues<TField>(TField field, int max) where TField : struct, Enum
        => new(ErrorCodes.FilterTooManyValues, $"The filter on field '{field}' has more than {max} values.", field.ToString());

    internal static ValidationError TooManyFilters(int max)
        => new(ErrorCodes.FiltersTooMany, $"A query can have at most {max} filters.");

    internal static ValidationError FiltersMalformed()
        => new(ErrorCodes.FiltersMalformed, "The filters must be a JSON array of filter objects.");

    internal static ValidationError TooManySorts(int max)
        => new(ErrorCodes.SortsTooMany, $"A query can have at most {max} sorts.");

    internal static ValidationError SortsMalformed()
        => new(ErrorCodes.SortsMalformed, "A sort could not be read. Each sort needs a field name and an optional direction.");

    internal static ValidationError DuplicateSort<TField>(TField field) where TField : struct, Enum
        => new(ErrorCodes.SortDuplicate, $"Field '{field}' appears in the sorts more than once.", field.ToString());

    internal static ValidationError SearchNotSupported()
        => new(ErrorCodes.SearchNotSupported, "Search is not supported for this query.");

    internal static ValidationError SearchTooLong(int max)
        => new(ErrorCodes.SearchTooLong, $"The search can be at most {max} characters.");

    internal static ValidationError CursorInvalid()
        => new(ErrorCodes.CursorInvalid, "The cursor is invalid, or was created for different sorts.");

    internal static ValidationError SpatialBoundsInvalid()
        => new(ErrorCodes.SpatialBoundsInvalid, "The spatial bounds are out of range or have a minimum greater than the maximum.");

    internal static ValidationError SpatialNotSupported()
        => new(ErrorCodes.SpatialNotSupported, "Spatial bounds are not supported for this query.");
}
