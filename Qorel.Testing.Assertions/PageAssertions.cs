using FluentAssertions;
using FluentAssertions.Primitives;

namespace Qorel.Testing.Assertions;

/// <summary>
/// Fluent assertions for <see cref="Page{T}"/>.
/// Obtain via <c>page.Should()</c> after adding <c>using Qorel.Testing.Assertions;</c>.
/// </summary>
/// <typeparam name="T">The item type of the page.</typeparam>
public sealed class PageAssertions<T>(Page<T> subject)
    : ReferenceTypeAssertions<Page<T>, PageAssertions<T>>(subject)
{
    /// <inheritdoc/>
    protected override string Identifier => "Page";

    /// <summary>
    /// Asserts that the page contains the expected number of items.
    /// </summary>
    public AndConstraint<PageAssertions<T>> HaveCount(
        int expected,
        string because = "",
        params object[] becauseArgs)
    {
        Subject.Count.Should().Be(expected, because, becauseArgs);
        return new AndConstraint<PageAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that <see cref="Page{T}.HasNextPage"/> is <c>true</c>.
    /// </summary>
    public AndConstraint<PageAssertions<T>> HaveNextPage(string because = "", params object[] becauseArgs)
    {
        Subject.HasNextPage.Should().BeTrue(because, becauseArgs);
        Subject.NextCursor.Should().NotBeNull(because, becauseArgs);
        return new AndConstraint<PageAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that <see cref="Page{T}.HasNextPage"/> is <c>false</c>.
    /// </summary>
    public AndConstraint<PageAssertions<T>> NotHaveNextPage(string because = "", params object[] becauseArgs)
    {
        Subject.HasNextPage.Should().BeFalse(because, becauseArgs);
        Subject.NextCursor.Should().BeNull(because, becauseArgs);
        return new AndConstraint<PageAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the page has no items.
    /// </summary>
    public AndConstraint<PageAssertions<T>> BeEmpty(string because = "", params object[] becauseArgs)
    {
        Subject.IsEmpty.Should().BeTrue(because, becauseArgs);
        return new AndConstraint<PageAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the page has at least one item.
    /// </summary>
    public AndConstraint<PageAssertions<T>> NotBeEmpty(string because = "", params object[] becauseArgs)
    {
        Subject.IsEmpty.Should().BeFalse(because, becauseArgs);
        return new AndConstraint<PageAssertions<T>>(this);
    }
}
