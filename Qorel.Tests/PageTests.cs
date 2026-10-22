namespace Qorel.Tests;

public class PageTests
{
    [Fact]
    public void HasNextPage_WhenNextCursorIsNonNull_IsTrue()
    {
        var page = new Page<int> { Items = [1, 2], NextCursor = "abc" };
        page.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_WhenNextCursorIsNull_IsFalse()
    {
        var page = new Page<int> { Items = [1], NextCursor = null };
        page.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void Count_MatchesItemsCount()
    {
        var page = new Page<string> { Items = ["a", "b", "c"] };
        page.Count.Should().Be(3);
    }

    [Fact]
    public void IsEmpty_WhenItemsEmpty_IsTrue()
    {
        var page = new Page<int> { Items = [] };
        page.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_WhenItemsPresent_IsFalse()
    {
        var page = new Page<int> { Items = [1] };
        page.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Empty_ReturnsZeroItemsAndNullCursor()
    {
        Page<int> page = Page<int>.Empty;
        page.Items.Should().BeEmpty();
        page.NextCursor.Should().BeNull();
        page.HasNextPage.Should().BeFalse();
        page.IsEmpty.Should().BeTrue();
    }
}
