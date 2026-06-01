using System.Collections.Frozen;

using Qorel.Schema;

namespace Qorel.Filters;

/// <summary>
/// Pre-defined sets of <see cref="FilterOperator"/> values for use when declaring
/// allowed operators on a field in <see cref="FieldSchema{TKey}"/>.
/// </summary>
/// <example>
/// <code>
/// // To allow only range operators on a price field:
/// For(ProductFields.Price).Filterable([.. FilterOperators.Comparison]).Sortable()
/// </code>
/// </example>
public static class FilterOperators
{
    /// <summary>
    /// Equality operators: <c>Equals</c>, <c>IsOneOf</c>, <c>IsNotOneOf</c>.
    /// </summary>
    public static readonly FrozenSet<FilterOperator> Equality =
        new[] { FilterOperator.Equals, FilterOperator.IsOneOf, FilterOperator.IsNotOneOf }.ToFrozenSet();

    /// <summary>
    /// Range operators: <c>GreaterThan</c>, <c>GreaterThanOrEquals</c>, <c>LessThan</c>, <c>LessThanOrEquals</c>.
    /// </summary>
    public static readonly FrozenSet<FilterOperator> Comparison =
        new[] { FilterOperator.GreaterThan, FilterOperator.GreaterThanOrEquals, FilterOperator.LessThan, FilterOperator.LessThanOrEquals }.ToFrozenSet();

    /// <summary>
    /// Text operators: <c>Contains</c>, <c>StartsWith</c>, <c>EndsWith</c>.
    /// </summary>
    public static readonly FrozenSet<FilterOperator> Text =
        new[] { FilterOperator.Contains, FilterOperator.StartsWith, FilterOperator.EndsWith }.ToFrozenSet();

    /// <summary>
    /// Null-check operators: <c>IsNull</c>, <c>IsNotNull</c>. Added automatically for nullable types.
    /// </summary>
    public static readonly FrozenSet<FilterOperator> NullCheck =
        new[] { FilterOperator.IsNull, FilterOperator.IsNotNull }.ToFrozenSet();

    /// <summary>
    /// Default operators for <see cref="bool"/> fields: equality only.
    /// </summary>
    public static readonly FrozenSet<FilterOperator> ForBool = Equality;

    /// <summary>
    /// Default operators for <see cref="Guid"/> fields: equality only.
    /// </summary>
    public static readonly FrozenSet<FilterOperator> ForGuid = Equality;

    /// <summary>
    /// Default operators for enum fields: equality only.
    /// </summary>
    public static readonly FrozenSet<FilterOperator> ForEnum = Equality;

    /// <summary>
    /// Default operators for numeric fields: equality and range.
    /// </summary>
    public static readonly FrozenSet<FilterOperator> ForNumber = Union(Equality, Comparison);

    /// <summary>
    /// Default operators for date/time fields: equality and range.
    /// </summary>
    public static readonly FrozenSet<FilterOperator> ForDate = Union(Equality, Comparison);

    /// <summary>
    /// Default operators for <see cref="string"/> fields: equality, substring search, and null checks.
    /// </summary>
    public static readonly FrozenSet<FilterOperator> ForString = Union(Equality, Text, NullCheck);

    private static FrozenSet<FilterOperator> Union(params FrozenSet<FilterOperator>[] sets)
        => sets.SelectMany(x => x).ToFrozenSet();
}
