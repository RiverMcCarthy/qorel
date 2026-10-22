using Qorel.Tests.Fixtures;

namespace Qorel.Tests;

public class QueryBuilderTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void WithPageSize_NotPositive_Throws(int size)
    {
        Func<QueryBuilder<TestField>> act = () => new QueryBuilder<TestField>().WithPageSize(size);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("pageSize");
    }

    [Fact]
    public void PageSize_IsLeftToTheOptions()
        => new QueryBuilder<TestField>().PageSize.Should().BeNull();

    [Fact]
    public void Settings_AreCarriedIntoTheQuery()
    {
        var bounds = BoundingBox.Create(10, 20, 30, 40);

        QueryBuilder<TestField> query = new QueryBuilder<TestField>()
            .WithPageSize(15)
            .WithCursor("cursor")
            .WithSpatialBounds(bounds)
            .WithSearch("red shoes");

        query.PageSize.Should().Be(15);
        query.Cursor.Should().Be("cursor");
        query.SpatialBounds.Should().Be(bounds);
        query.Search.Should().Be("red shoes");
    }

    [Fact]
    public void Where_AddsEveryFilter()
    {
        QueryBuilder<TestField> query = new QueryBuilder<TestField>()
            .Where(TestFields.Price.GreaterThan(10m))
            .Where(TestFields.Stock.LessThan(5), TestFields.Name.Contains("x"));

        query.Filters.Select(f => f.Field).Should().Equal(TestField.Price, TestField.Stock, TestField.Name);
    }

    [Fact]
    public void Where_NullFilter_Throws()
    {
        Func<QueryBuilder<TestField>> act = () => new QueryBuilder<TestField>().Where((Filter<TestField>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ThenBy_AddsSortsInOrder()
    {
        QueryBuilder<TestField> query = new QueryBuilder<TestField>()
            .OrderByDescending(TestFields.Price)
            .ThenBy(TestFields.Name);

        query.Sorts.Should().Equal(new Sort<TestField>(TestField.Price, SortDirection.Desc), new Sort<TestField>(TestField.Name, SortDirection.Asc));
    }

    [Fact]
    public void OrderBy_ReplacesEarlierSorts()
    {
        QueryBuilder<TestField> query = new QueryBuilder<TestField>()
            .OrderBy(TestFields.Price)
            .ThenBy(TestFields.Name)
            .OrderByDescending(TestFields.Stock);

        query.Sorts.Should().Equal(new Sort<TestField>(TestField.Stock, SortDirection.Desc));
    }

    [Fact]
    public void SortingByAFieldTwice_Throws()
    {
        Func<QueryBuilder<TestField>> act = () => new QueryBuilder<TestField>().OrderBy(TestFields.Price).ThenByDescending(TestFields.Price);

        act.Should().Throw<ArgumentException>().WithMessage("*already sorted by 'Price'*");
    }

    [Fact]
    public void NullableFields_CanBeSorted()
    {
        QueryBuilder<TestField> query = new QueryBuilder<TestField>()
            .OrderByDescending(TestFields.Score)
            .ThenBy(TestFields.Price);

        query.Sorts.Should().Equal(new Sort<TestField>(TestField.Score, SortDirection.Desc), new Sort<TestField>(TestField.Price, SortDirection.Asc));
    }
}
