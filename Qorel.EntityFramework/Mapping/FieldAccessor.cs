using System.Linq.Expressions;
using System.Reflection;

using Qorel.EntityFramework.Extensions;
using Qorel.Filters;

namespace Qorel.EntityFramework.Mapping;

/// <inheritdoc/>
internal sealed class FieldAccessor<TEntity, TValue>(Expression<Func<TEntity, TValue>> selector)
    : IFieldAccessor<TEntity>
{
    // MethodInfo instances resolved once per TValue at type initialisation.
    private static readonly MethodInfo _enumerableContains =
        typeof(Enumerable)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TValue));

    private static readonly MethodInfo? _stringContains = typeof(TValue) == typeof(string)
        ? typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])
        : null;

    private static readonly MethodInfo? _stringStartsWith = typeof(TValue) == typeof(string)
        ? typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])
        : null;

    private static readonly MethodInfo? _stringEndsWith = typeof(TValue) == typeof(string)
        ? typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])
        : null;

    public Type Type => typeof(TValue);

    public IOrderedQueryable<TEntity> OrderBy(IQueryable<TEntity> source, bool descending)
        => descending ? source.OrderByDescending(selector) : source.OrderBy(selector);

    public IOrderedQueryable<TEntity> ThenBy(IOrderedQueryable<TEntity> source, bool descending)
        => descending ? source.ThenByDescending(selector) : source.ThenBy(selector);

    public Expression Inline(ParameterExpression param)
        => selector.InlineBody(param);

    public Expression<Func<TEntity, bool>> BuildEqualityPredicate(object value)
        => Expression.Lambda<Func<TEntity, bool>>(
            Expression.Equal(selector.Body, Expression.Constant((TValue)value, typeof(TValue))),
            selector.Parameters);

    public Expression<Func<TEntity, bool>> BuildPredicate<TKey>(IFilter<TKey> filter)
        where TKey : Enum
    {
        Expression body = filter.Operator switch
        {
            FilterOperator.IsNull => NullCheckExpression(negate: false),
            FilterOperator.IsNotNull => NullCheckExpression(negate: true),
            FilterOperator.Equals => Expression.Equal(selector.Body, ScalarConstant(filter)),
            FilterOperator.Contains => StringMethodExpression(filter, _stringContains, nameof(string.Contains)),
            FilterOperator.StartsWith => StringMethodExpression(filter, _stringStartsWith, nameof(string.StartsWith)),
            FilterOperator.EndsWith => StringMethodExpression(filter, _stringEndsWith, nameof(string.EndsWith)),
            FilterOperator.GreaterThan => Expression.GreaterThan(selector.Body, ScalarConstant(filter)),
            FilterOperator.GreaterThanOrEquals => Expression.GreaterThanOrEqual(selector.Body, ScalarConstant(filter)),
            FilterOperator.LessThan => Expression.LessThan(selector.Body, ScalarConstant(filter)),
            FilterOperator.LessThanOrEquals => Expression.LessThanOrEqual(selector.Body, ScalarConstant(filter)),
            FilterOperator.IsOneOf => MembershipExpression(filter, negate: false),
            FilterOperator.IsNotOneOf => MembershipExpression(filter, negate: true),
            _ => throw new NotSupportedException($"Operator '{filter.Operator}' is not supported."),
        };

        return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
    }

    private BinaryExpression NullCheckExpression(bool negate)
    {
        ConstantExpression nullConstant = Expression.Constant(null, typeof(TValue));
        return negate
            ? Expression.NotEqual(selector.Body, nullConstant)
            : Expression.Equal(selector.Body, nullConstant);
    }

    private static ConstantExpression ScalarConstant<TKey>(IFilter<TKey> filter)
        where TKey : Enum
    {
        if (filter is not ScalarFilter<TKey, TValue> scalar)
        {
            throw new InvalidOperationException($"Expected a scalar filter for field '{filter.FieldKey}' but received {filter.GetType().Name}.");
        }

        return Expression.Constant(scalar.Value, typeof(TValue));
    }

    private MethodCallExpression StringMethodExpression<TKey>(IFilter<TKey> filter, MethodInfo? method, string operatorName)
        where TKey : Enum
    {
        if (method is null)
        {
            throw new InvalidOperationException($"The {operatorName} operator requires a string field, but '{filter.FieldKey}' is '{typeof(TValue).Name}'.");
        }

        return Expression.Call(selector.Body, method, ScalarConstant(filter));
    }

    private Expression MembershipExpression<TKey>(IFilter<TKey> filter, bool negate) where TKey : Enum
    {
        if (filter is not CollectionFilter<TKey, TValue> collection)
        {
            throw new InvalidOperationException($"Expected a collection filter for field '{filter.FieldKey}' but received {filter.GetType().Name}.");
        }

        MethodCallExpression call = Expression.Call(
            _enumerableContains,
            Expression.Constant(collection.Values),
            selector.Body);

        return negate ? Expression.Not(call) : call;
    }
}
