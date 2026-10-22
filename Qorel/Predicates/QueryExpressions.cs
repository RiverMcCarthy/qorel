using System.Linq.Expressions;
using System.Reflection;

namespace Qorel;

/// <summary>
/// Expression-tree building blocks shared by filters and keyset paging.
/// </summary>
internal static class QueryExpressions
{
    /// <summary>
    /// Wraps <paramref name="value"/> so EF Core sends it as a SQL parameter.
    /// </summary>
    /// <remarks>
    /// EF Core writes a <see cref="ConstantExpression"/> straight into the SQL, so every new value would be a new query
    /// to compile and a new plan for the database to cache. Reading the value from a property of a constant object is
    /// the same shape as a captured variable in a hand-written LINQ query, which EF Core turns into a parameter.
    /// </remarks>
    public static Expression Parameter<T>(T value)
        => Expression.Property(Expression.Constant(new ParameterValue<T>(value)), nameof(ParameterValue<T>.Value));

    /// <summary>
    /// A null of <paramref name="type"/>.
    /// </summary>
    public static ConstantExpression Null(Type type) => Expression.Constant(null, type);

    /// <summary>
    /// Compares <paramref name="left"/> with <paramref name="right"/> using a comparison such as
    /// <see cref="ExpressionType.GreaterThan"/>.
    /// </summary>
    /// <remarks>
    /// C# defines <c>&gt;</c> and <c>&lt;</c> for numbers and dates, but not for <see cref="string"/>,
    /// <see cref="Guid"/> or <see cref="bool"/>. Those are compared as <c>left.CompareTo(right) &gt; 0</c>, which EF Core
    /// translates back to a plain SQL comparison.
    /// </remarks>
    /// <exception cref="NotSupportedException">Thrown when the type cannot be compared.</exception>
    public static Expression Compare(ExpressionType comparison, Expression left, Expression right)
    {
        Type? underlying = Nullable.GetUnderlyingType(left.Type);

        // C# lifts the operators of a nullable number or date, so they can be used as they are.
        if (HasComparisonOperators(underlying ?? left.Type))
            return Expression.MakeBinary(comparison, left, right);

        // Nullable<T> has no CompareTo of its own, so compare the values. SQL still never matches a null.
        if (underlying is not null)
        {
            left = Expression.Property(left, nameof(Nullable<int>.Value));
            right = Expression.Property(right, nameof(Nullable<int>.Value));
        }

        MethodInfo compareTo = left.Type.GetMethod(nameof(IComparable.CompareTo), [left.Type])
            ?? throw new NotSupportedException($"'{left.Type.Name}' values cannot be compared.");

        return Expression.MakeBinary(comparison, Expression.Call(left, compareTo, right), Expression.Constant(0));
    }

    private static bool HasComparisonOperators(Type type)
        => (type.IsPrimitive && type != typeof(bool))
            || type.GetMethod("op_GreaterThan", BindingFlags.Public | BindingFlags.Static, [type, type]) is not null;

    // A class, not a struct, because EF Core only turns member access on a reference into a parameter.
    private sealed class ParameterValue<T>(T value)
    {
        public T Value { get; } = value;
    }
}
