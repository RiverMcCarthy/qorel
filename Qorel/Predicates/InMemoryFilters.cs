using System.Diagnostics;

namespace Qorel;

/// <summary>
/// Turns a filter into a plain delegate, for data that is already in memory.
/// </summary>
/// <remarks>
/// It follows the same rules as <see cref="FilterExpressions"/>, so in-memory results match the database's: a null never
/// matches a text or comparison operator, and nothing here can throw on a null value. Text is compared ordinally,
/// ignoring case when the field is set to.
/// </remarks>
internal static class InMemoryFilters
{
    public static Func<TEntity, bool> Build<TEntity, TField, TValue>(
        Func<TEntity, TValue> getValue,
        FieldFilter<TField, TValue> filter,
        bool ignoreCase)
        where TField : struct, Enum
    {
        // Only string fields can ignore case, so the string comparer is always an IEqualityComparer<TValue> here.
        IEqualityComparer<TValue> equality = ignoreCase
            ? (IEqualityComparer<TValue>)StringComparer.OrdinalIgnoreCase
            : EqualityComparer<TValue>.Default;

        StringComparison textComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        return filter.Operator switch
        {
            FilterOperator.IsNull => entity => getValue(entity) is null,
            FilterOperator.IsNotNull => entity => getValue(entity) is not null,
            FilterOperator.Equals => entity => equality.Equals(getValue(entity), filter.Value),
            FilterOperator.IsOneOf => IsOneOf(getValue, filter.Values, equality),
            FilterOperator.IsNotOneOf => Not(IsOneOf(getValue, filter.Values, equality)),
            FilterOperator.Contains => Text(getValue, filter.Value, (value, text) => value.Contains(text, textComparison)),
            FilterOperator.StartsWith => Text(getValue, filter.Value, (value, text) => value.StartsWith(text, textComparison)),
            FilterOperator.EndsWith => Text(getValue, filter.Value, (value, text) => value.EndsWith(text, textComparison)),
            FilterOperator.GreaterThan => Compare(getValue, filter.Value, result => result > 0),
            FilterOperator.GreaterThanOrEquals => Compare(getValue, filter.Value, result => result >= 0),
            FilterOperator.LessThan => Compare(getValue, filter.Value, result => result < 0),
            FilterOperator.LessThanOrEquals => Compare(getValue, filter.Value, result => result <= 0),
            _ => throw new UnreachableException($"Unknown operator '{filter.Operator}'."),
        };
    }

    private static Func<TEntity, bool> IsOneOf<TEntity, TValue>(
        Func<TEntity, TValue> getValue,
        IReadOnlyList<TValue> values,
        IEqualityComparer<TValue> equality)
    {
        var set = new HashSet<TValue>(values, equality);
        return entity => set.Contains(getValue(entity));
    }

    private static Func<TEntity, bool> Text<TEntity, TValue>(
        Func<TEntity, TValue> getValue,
        TValue target,
        Func<string, string, bool> matches)
    {
        if (target is not string text)
            return _ => false;

        return entity => getValue(entity) is string value && matches(value, text);
    }

    // Comparer<T>.Default sorts null first, but SQL never matches a null in a comparison, so nulls are checked here.
    private static Func<TEntity, bool> Compare<TEntity, TValue>(
        Func<TEntity, TValue> getValue,
        TValue target,
        Func<int, bool> matches)
    {
        if (target is null)
            return _ => false;

        Comparer<TValue> comparer = Comparer<TValue>.Default;
        return entity => getValue(entity) is { } value && matches(comparer.Compare(value, target));
    }

    private static Func<TEntity, bool> Not<TEntity>(Func<TEntity, bool> predicate) => entity => !predicate(entity);
}
