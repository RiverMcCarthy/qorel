using Qorel.Tests.Fixtures;

namespace Qorel.Tests;

public class FieldTests
{
    [Fact]
    public void Filters_CarryTheFieldAndOperator()
    {
        Filter<TestField> filter = TestFields.Price.GreaterThanOrEquals(10m);

        filter.Field.Should().Be(TestField.Price);
        filter.Operator.Should().Be(FilterOperator.GreaterThanOrEquals);
    }

    [Fact]
    public void IsOneOf_TakesValuesOrACollection()
        => TestFields.Stock.IsOneOf(1, 2).Should().Be(TestFields.Stock.IsOneOf(new List<int> { 2, 1 }));

    [Fact]
    public void IsNull_OnANullableField_Works()
        => TestFields.Score.IsNull().CacheKey.Should().Be("Score.IsNull");

    [Fact]
    public void IsNull_OnAFieldThatCannotBeNull_Throws()
    {
        Func<Filter<TestField>> act = () => TestFields.Stock.IsNull();

        act.Should().Throw<InvalidOperationException>().WithMessage("*never null*");
    }

    [Fact]
    public void IsOneOf_NullCollection_Throws()
    {
        Func<Filter<TestField>> act = () => TestFields.Stock.IsOneOf((IEnumerable<int>)null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
