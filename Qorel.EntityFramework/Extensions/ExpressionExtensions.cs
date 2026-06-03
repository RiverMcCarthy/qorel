using System.Linq.Expressions;

namespace Qorel.EntityFramework.Extensions;

internal static class ExpressionExtensions
{
    /// <summary>
    /// Combines two boolean predicates, reusing the left predicate's parameter.
    /// </summary>
    internal static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        ParameterExpression param = left.Parameters[0];
        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(left.Body, right.InlineBody(param)),
            param);
    }

    /// <summary>
    /// Returns the body of a lambda with its parameter replaced by <paramref name="replacement"/>.
    /// Used to inline sub-expressions into a shared parameter context.
    /// </summary>
    internal static Expression InlineBody(this LambdaExpression lambda, ParameterExpression replacement)
        => new ParameterSubstitutionVisitor(lambda.Parameters[0], replacement).Visit(lambda.Body)!;

    private sealed class ParameterSubstitutionVisitor(ParameterExpression from, ParameterExpression to)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == from ? to : base.VisitParameter(node);
    }
}
