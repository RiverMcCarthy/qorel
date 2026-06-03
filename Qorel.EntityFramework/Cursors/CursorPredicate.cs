using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

using Qorel.EntityFramework.Mapping;
using Qorel.Exceptions;
using Qorel.Querying;

namespace Qorel.EntityFramework.Cursors;

/// <summary>
/// Builds the keyset pagination WHERE predicate from a decoded <see cref="CursorToken"/>.
/// Implements the OR-of-ANDs pattern for correct multi-column keyset pagination.
/// </summary>
internal static class CursorPredicate
{
    private static readonly MethodInfo _guidCompareTo =
        typeof(Guid).GetMethod(nameof(Guid.CompareTo), [typeof(Guid)])!;

    private const string _invalidCursorMessage = "The pagination cursor is invalid or has been tampered with.";

    /// <summary>
    /// Builds a predicate that returns only entities after the cursor under the given sort clauses.
    /// </summary>
    public static Expression<Func<TEntity, bool>> Build<TEntity, TKey>(
        CursorToken cursor,
        IReadOnlyList<ISort<TKey>> sorts,
        FieldMap<TEntity, TKey> fieldMap)
        where TEntity : class
        where TKey : Enum
    {
        if (cursor.Values.Length != sorts.Count)
            throw new InvalidCursorException("Cursor value count does not match the number of sort columns.");

        ParameterExpression param = Expression.Parameter(typeof(TEntity), "x");

        // OR-of-ANDs pattern:
        // (field0 > v0)
        // OR (field0 = v0 AND field1 > v1)
        // OR (field0 = v0 AND field1 = v1 AND field2 > v2)
        var clauses = new List<Expression>(sorts.Count);
        Expression? equalityPrefix = null;

        for (var i = 0; i < sorts.Count; i++)
        {
            IFieldAccessor<TEntity> accessor = fieldMap.Get(sorts[i].FieldKey);
            object value;
            try
            {
                value = JsonSerializer.Deserialize(cursor.Values[i], accessor.Type)
                    ?? throw new InvalidCursorException(_invalidCursorMessage);
            }
            catch (JsonException ex)
            {
                throw new InvalidCursorException(_invalidCursorMessage, ex);
            }

            Expression member = accessor.Inline(param);
            ConstantExpression constant = Expression.Constant(value, accessor.Type);

            var ascending = sorts[i].Direction == SortDirection.Asc;
            BinaryExpression comparison = BuildComparison(member, constant, accessor.Type, ascending);
            Expression clause = equalityPrefix is null
                ? comparison
                : Expression.AndAlso(equalityPrefix, comparison);

            clauses.Add(clause);

            BinaryExpression equality = Expression.Equal(member, constant);
            equalityPrefix = equalityPrefix is null
                ? equality
                : Expression.AndAlso(equalityPrefix, equality);
        }

        return Expression.Lambda<Func<TEntity, bool>>(clauses.Aggregate(Expression.OrElse), param);
    }

    private static BinaryExpression BuildComparison(Expression member, Expression constant, Type type, bool ascending)
    {
        if (type == typeof(Guid))
        {
            // PostgreSQL specific: Guid comparison requires CompareTo.
            MethodCallExpression compareTo = Expression.Call(member, _guidCompareTo, constant);
            return ascending
                ? Expression.GreaterThan(compareTo, Expression.Constant(0))
                : Expression.LessThan(compareTo, Expression.Constant(0));
        }

        return ascending
            ? Expression.GreaterThan(member, constant)
            : Expression.LessThan(member, constant);
    }
}
