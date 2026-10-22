namespace Qorel.Testing.Assertions;

/// <summary>
/// Entry points for Qorel FluentAssertions extensions.
/// </summary>
/// <example>
/// <code>
/// result.Should().Succeed();
/// result.Should().FailWithCode(ErrorCodes.FieldUnknown);
/// page.Should().HaveCount(10).And.HaveNextPage();
/// </code>
/// </example>
public static class QorelAssertionsExtensions
{
    /// <summary>
    /// Returns an assertion object for a <see cref="QueryResult{T}"/>.
    /// </summary>
    public static QueryResultAssertions<T> Should<T>(this QueryResult<T> subject)
        => new(subject);

    /// <summary>
    /// Returns an assertion object for a <see cref="Page{T}"/>.
    /// </summary>
    public static PageAssertions<T> Should<T>(this Page<T> subject)
        => new(subject);
}
