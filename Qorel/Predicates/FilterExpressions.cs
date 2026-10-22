using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Qorel;

/// <summary>
/// Turns a filter into an expression tree that a LINQ provider such as EF Core translates to SQL.
/// </summary>
/// <remarks>
/// <para>Every filter value is sent as a parameter (see <see cref="QueryExpressions.Parameter{T}"/>).</para>
/// <para>
/// A null filter value never matches a text or comparison operator, the same as in SQL and
/// <see cref="InMemoryFilters"/>. A field that ignores case is compared as <c>LOWER(column)</c> against a lowercased value.
/// </para>
/// </remarks>
internal static class FilterExpressions
{
    private static readonly MethodInfo _toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
    private static readonly MethodInfo _contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
    private static readonly MethodInfo _startsWith = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
    private static readonly MethodInfo _endsWith = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;
    private static readonly MethodInfo _enumerableContains = typeof(Enumerable).GetMethods()
        .Single(method => method.Name == nameof(Enumerable.Contains) && method.GetParameters().Length == 2);

    private static readonly Expression _noMatch = Expression.Constant(false);

    public static Expression<Func<TEntity, bool>> Build<TEntity, TField, TValue>(
        Expression<Func<TEntity, TValue>> selector,
        FieldFilter<TField, TValue> filter,
        bool ignoreCase)
        where TField : struct, Enum
    {
        Expression field = selector.Body;

        Expression body = filter.Operator switch
        {
            FilterOperator.IsNull => Expression.Equal(field, QueryExpressions.Null(typeof(TValue))),
            FilterOperator.IsNotNull => Expression.NotEqual(field, QueryExpressions.Null(typeof(TValue))),
            FilterOperator.Equals => Expression.Equal(FoldCase(field, ignoreCase), QueryExpressions.Parameter(FoldCase(filter.Value, ignoreCase))),
            FilterOperator.IsOneOf => IsOneOf(field, filter.Values, ignoreCase),
            FilterOperator.IsNotOneOf => Expression.Not(IsOneOf(field, filter.Values, ignoreCase)),
            FilterOperator.Contains => Text(_contains, field, filter.Value, ignoreCase),
            FilterOperator.StartsWith => Text(_startsWith, field, filter.Value, ignoreCase),
            FilterOperator.EndsWith => Text(_endsWith, field, filter.Value, ignoreCase),
            FilterOperator.GreaterThan => Compare(ExpressionType.GreaterThan, field, filter.Value),
            FilterOperator.GreaterThanOrEquals => Compare(ExpressionType.GreaterThanOrEqual, field, filter.Value),
            FilterOperator.LessThan => Compare(ExpressionType.LessThan, field, filter.Value),
            FilterOperator.LessThanOrEquals => Compare(ExpressionType.LessThanOrEqual, field, filter.Value),
            _ => throw new UnreachableException($"Unknown operator '{filter.Operator}'."),
        };

        return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
    }

    private static MethodCallExpression IsOneOf<TValue>(Expression field, IReadOnlyList<TValue> values, bool ignoreCase)
    {
        TValue[] folded = [.. values.Select(value => FoldCase(value, ignoreCase))];
        return Expression.Call(_enumerableContains.MakeGenericMethod(typeof(TValue)), QueryExpressions.Parameter(folded), FoldCase(field, ignoreCase));
    }

    private static Expression Text<TValue>(MethodInfo method, Expression field, TValue value, bool ignoreCase)
        => value is null
            ? _noMatch
            : Expression.Call(FoldCase(field, ignoreCase), method, QueryExpressions.Parameter(FoldCase(value, ignoreCase)));

    private static Expression Compare<TValue>(ExpressionType comparison, Expression field, TValue value)
        => value is null
            ? _noMatch
            : QueryExpressions.Compare(comparison, field, QueryExpressions.Parameter(value));

    private static Expression FoldCase(Expression field, bool ignoreCase)
        => ignoreCase ? Expression.Call(field, _toLower) : field;

    // Only string fields can ignore case, so a string here is always a TValue. Casting through object turns it back.
    private static TValue FoldCase<TValue>(TValue value, bool ignoreCase)
        => ignoreCase && value is string text ? (TValue)(object)text.ToLowerInvariant() : value;
}
