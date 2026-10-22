using System.Collections.Frozen;

namespace Qorel;

/// <summary>
/// Groups of <see cref="FilterOperator"/> values, for narrowing what a field allows.
/// </summary>
/// <example>
/// <code>
/// model.Map(ProductFields.Price, e => e.Price).Filterable(FilterOperators.Comparison);
/// </code>
/// </example>
public static class FilterOperators
{
    /// <summary>
    /// <c>Equals</c>, <c>IsOneOf</c>, and <c>IsNotOneOf</c>.
    /// </summary>
    public static IReadOnlySet<FilterOperator> Equality { get; } =
        new[] { FilterOperator.Equals, FilterOperator.IsOneOf, FilterOperator.IsNotOneOf }.ToFrozenSet();

    /// <summary>
    /// <c>GreaterThan</c>, <c>GreaterThanOrEquals</c>, <c>LessThan</c>, and <c>LessThanOrEquals</c>.
    /// </summary>
    public static IReadOnlySet<FilterOperator> Comparison { get; } =
        new[] { FilterOperator.GreaterThan, FilterOperator.GreaterThanOrEquals, FilterOperator.LessThan, FilterOperator.LessThanOrEquals }.ToFrozenSet();

    /// <summary>
    /// <c>Contains</c>, <c>StartsWith</c>, and <c>EndsWith</c>.
    /// </summary>
    public static IReadOnlySet<FilterOperator> Text { get; } =
        new[] { FilterOperator.Contains, FilterOperator.StartsWith, FilterOperator.EndsWith }.ToFrozenSet();

    /// <summary>
    /// <c>IsNull</c> and <c>IsNotNull</c>.
    /// </summary>
    public static IReadOnlySet<FilterOperator> NullCheck { get; } =
        new[] { FilterOperator.IsNull, FilterOperator.IsNotNull }.ToFrozenSet();
}
