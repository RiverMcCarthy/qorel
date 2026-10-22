using System.Linq.Expressions;

namespace Qorel;

/// <summary>
/// Rebinds a lambda's body to a different parameter, so expressions written separately can be combined into one lambda.
/// </summary>
internal sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
{
    /// <summary>
    /// Returns the body of <paramref name="lambda"/>, reading from <paramref name="parameter"/> instead of its own parameter.
    /// </summary>
    public static Expression Replace(LambdaExpression lambda, ParameterExpression parameter)
        => new ParameterReplacer(lambda.Parameters[0], parameter).Visit(lambda.Body);

    protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
}
