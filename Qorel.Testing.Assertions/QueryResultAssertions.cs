using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Qorel.Testing.Assertions;

/// <summary>
/// Fluent assertions for <see cref="QueryResult{T}"/>.
/// Obtain via <c>result.Should()</c> after adding <c>using Qorel.Testing.Assertions;</c>.
/// </summary>
/// <typeparam name="T">The value type of the result.</typeparam>
public sealed class QueryResultAssertions<T>(QueryResult<T> subject)
    : ReferenceTypeAssertions<QueryResult<T>, QueryResultAssertions<T>>(subject)
{
    /// <inheritdoc/>
    protected override string Identifier => "QueryResult";

    /// <summary>
    /// Asserts that the result is a success.
    /// </summary>
    public AndConstraint<QueryResultAssertions<T>> Succeed(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.IsSuccess)
            .FailWith("Expected {context:QueryResult} to succeed{reason}, but it failed with: {0}.", Describe(Subject.Errors));

        return new AndConstraint<QueryResultAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the result is a failure.
    /// </summary>
    public AndConstraint<QueryResultAssertions<T>> Fail(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.IsFailure)
            .FailWith("Expected {context:QueryResult} to fail{reason}, but it succeeded.");

        return new AndConstraint<QueryResultAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the result failed with an error that has <paramref name="errorCode"/>.
    /// </summary>
    /// <param name="errorCode">An <see cref="ErrorCodes"/> constant.</param>
    /// <param name="because">Reason appended to failure messages.</param>
    /// <param name="becauseArgs">Format arguments for <paramref name="because"/>.</param>
    public AndConstraint<QueryResultAssertions<T>> FailWithCode(string errorCode, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Errors.Any(error => error.Code == errorCode))
            .FailWith(
                "Expected {context:QueryResult} to fail with code {0}{reason}, but it {1}.",
                errorCode,
                Subject.IsSuccess ? "succeeded" : $"failed with: {Describe(Subject.Errors)}");

        return new AndConstraint<QueryResultAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the result is a success, and continues with assertions on its value.
    /// </summary>
    public AndWhichConstraint<QueryResultAssertions<T>, T> SucceedWith(string because = "", params object[] becauseArgs)
    {
        Succeed(because, becauseArgs);
        return new AndWhichConstraint<QueryResultAssertions<T>, T>(this, Subject.Value);
    }

    private static string Describe(IEnumerable<ValidationError> errors)
        => string.Join("; ", errors.Select(error => $"{error.Code} ({error.Message})"));
}
