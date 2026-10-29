using System.Linq.Expressions;

namespace Qorel;

/// <summary>
/// Builds the condition that skips the pages a client has already seen: every item that sorts after the last item of
/// the previous page.
/// </summary>
/// <remarks>
/// An item comes after if it is after in the first key, or tied on the first and after in the second, and so on:
/// <code>
/// (k0 &gt; v0) OR (k0 = v0 AND k1 &gt; v1) OR (k0 = v0 AND k1 = v1 AND k2 &gt; v2)
/// </code>
/// Descending keys use &lt; instead of &gt;.
/// </remarks>
internal static class KeysetPredicate
{
    /// <summary>
    /// Matches the items that sort after <paramref name="previous"/>, the previous page's last value in each key.
    /// </summary>
    public static Expression<Func<TEntity, bool>> After<TEntity, TField>(
        IReadOnlyList<SortKey<TEntity, TField>> keys,
        IReadOnlyList<object?> previous)
        where TEntity : class
        where TField : struct, Enum
    {
        ParameterExpression entity = Expression.Parameter(typeof(TEntity), "entity");

        Expression? after = null;
        Expression? tiedSoFar = null;

        for (int i = 0; i < keys.Count; i++)
        {
            SortKey<TEntity, TField> key = keys[i];
            Expression value = key.Field.Read(entity);

            // One parameter per key, used by both comparisons. Null when the previous item's value was null.
            Expression? previousValue = previous[i] is null ? null : key.Field.Parameter(previous[i]);

            if (IsAfter(key, value, previousValue) is { } isAfter)
                after = Or(after, And(tiedSoFar, isAfter));

            tiedSoFar = And(tiedSoFar, Expression.Equal(value, previousValue ?? QueryExpressions.Null(value.Type)));
        }

        return Expression.Lambda<Func<TEntity, bool>>(after ?? Expression.Constant(false), entity);
    }

    // The condition for an item being strictly after the previous one in this key, or null if no item can be.
    // Nulls sort last ascending and first descending (see MappedField.OrderBy), which decides where they fall here.
    private static Expression? IsAfter<TEntity, TField>(SortKey<TEntity, TField> key, Expression value, Expression? previousValue)
        where TEntity : class
        where TField : struct, Enum
    {
        if (previousValue is null)
        {
            // Ascending, nothing comes after the nulls at the end. Descending, every value comes after the nulls at the start.
            return key.Descending ? Expression.NotEqual(value, QueryExpressions.Null(value.Type)) : null;
        }

        Expression isAfter = QueryExpressions.Compare(key.Descending ? ExpressionType.LessThan : ExpressionType.GreaterThan, value, previousValue);

        // Ascending, the nulls are still to come after every value.
        return key.Field.CanBeNull && !key.Descending
            ? Expression.OrElse(isAfter, Expression.Equal(value, QueryExpressions.Null(value.Type)))
            : isAfter;
    }

    private static Expression And(Expression? left, Expression right) => left is null ? right : Expression.AndAlso(left, right);

    private static Expression Or(Expression? left, Expression right) => left is null ? right : Expression.OrElse(left, right);
}
