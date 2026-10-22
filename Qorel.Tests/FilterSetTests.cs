using System.Globalization;

using Qorel.Tests.Fixtures;

namespace Qorel.Tests;

public class FilterSetTests
{
    private readonly TestQueryModel _model = new();

    private FilterSet<TestField> Parse(string json) => _model.ParseFilters(json).Value;

    // --- Key stability --------------------------------------------------------

    [Fact]
    public void Key_SameFiltersInAnyOrder_IsTheSame()
    {
        FilterSet<TestField> first = Parse("""
            [
              { "field": "Price", "operator": "LessThan", "value": 5000 },
              { "field": "Category", "operator": "Equals", "value": "Beta" }
            ]
            """);
        FilterSet<TestField> second = Parse("""
            [
              { "field": "Category", "operator": "Equals", "value": "Beta" },
              { "field": "Price", "operator": "LessThan", "value": 5000 }
            ]
            """);

        second.CacheKey.Should().Be(first.CacheKey);
        second.Equals(first).Should().BeTrue();
    }

    [Fact]
    public void Filter_HasTheSameKeyOnItsOwn()
        => TestFields.Price.LessThan(5000m).CacheKey.Should().Be("Price.LessThan=5000");

    [Fact]
    public void Filters_WithTheSameKey_AreEqual()
        => TestFields.Price.LessThan(5000m).Should().Be(TestFields.Price.LessThan(5000.00m));

    [Fact]
    public void Key_IsReadable()
    {
        var set = FilterSet<TestField>.Create(
        [
            TestFields.Price.LessThan(5000m),
            TestFields.Category.EqualTo(TestCategory.Beta),
        ]);

        set.CacheKey.Should().Be("""Category.Equals="Beta";Price.LessThan=5000""");
    }

    [Theory]
    [InlineData("5000", "5000.0")]
    [InlineData("5000", "5000.000")]
    [InlineData("5000", "5e3")]
    [InlineData("0.5", "0.50")]
    public void Key_EquivalentNumbers_GiveTheSameKey(string left, string right)
    {
        FilterSet<TestField> first = Parse($$"""[{ "field": "Price", "operator": "Equals", "value": {{left}} }]""");
        FilterSet<TestField> second = Parse($$"""[{ "field": "Price", "operator": "Equals", "value": {{right}} }]""");

        second.CacheKey.Should().Be(first.CacheKey);
    }

    [Fact]
    public void Key_SameInstantInDifferentOffsets_GiveTheSameKey()
    {
        FilterSet<TestField> utc = Parse("""[{ "field": "CreatedAt", "operator": "GreaterThan", "value": "2024-01-01T00:00:00Z" }]""");
        FilterSet<TestField> plusOne = Parse("""[{ "field": "CreatedAt", "operator": "GreaterThan", "value": "2024-01-01T01:00:00+01:00" }]""");

        plusOne.CacheKey.Should().Be(utc.CacheKey);
    }

    [Fact]
    public void Key_IsOneOfInAnyOrderOrWithDuplicates_IsTheSame()
    {
        var first = FilterSet<TestField>.Create([TestFields.Stock.IsOneOf(1, 2, 3)]);
        var second = FilterSet<TestField>.Create([TestFields.Stock.IsOneOf(3, 1, 2, 1)]);

        second.CacheKey.Should().Be(first.CacheKey);
    }

    [Fact]
    public void Key_DoesNotDependOnCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var set = FilterSet<TestField>.Create([TestFields.Price.GreaterThan(1234.5m)]);

            set.CacheKey.Should().Be("Price.GreaterThan=1234.5");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Key_DifferentValues_GiveDifferentKeys()
    {
        var first = FilterSet<TestField>.Create([TestFields.Price.LessThan(10m)]);
        var second = FilterSet<TestField>.Create([TestFields.Price.LessThan(11m)]);

        second.CacheKey.Should().NotBe(first.CacheKey);
    }

    [Fact]
    public void Key_StringContainingSeparators_CannotPassForAnotherFilter()
    {
        // Without quoting, this one filter could read the same as two separate filters.
        var sneaky = FilterSet<TestField>.Create([TestFields.Name.EqualTo("a\";Price.LessThan=5")]);
        var two = FilterSet<TestField>.Create([TestFields.Name.EqualTo("a"), TestFields.Price.LessThan(5m)]);

        sneaky.CacheKey.Should().NotBe(two.CacheKey);
    }

    // --- Contents -------------------------------------------------------------

    [Fact]
    public void Create_DuplicateFilters_AreDropped()
    {
        var set = FilterSet<TestField>.Create(
        [
            TestFields.Stock.GreaterThan(5),
            TestFields.Stock.GreaterThan(5),
        ]);

        set.Should().ContainSingle();
    }


    // --- Empty ----------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("[]")]
    public void ParseFilterSet_NoFilters_ReturnsEmpty(string? json)
    {
        FilterSet<TestField> set = _model.ParseFilters(json).Value;

        set.Should().BeSameAs(FilterSet<TestField>.Empty);
        set.CacheKey.Should().BeEmpty();
    }

    // --- Equality -------------------------------------------------------------

    [Fact]
    public void Equality_WorksAsDictionaryKey()
    {
        var cache = new Dictionary<FilterSet<TestField>, string>
        {
            [FilterSet<TestField>.Create([TestFields.Stock.GreaterThan(5), TestFields.Name.Contains("x")])] = "cached",
        };

        var lookup = FilterSet<TestField>.Create([TestFields.Name.Contains("x"), TestFields.Stock.GreaterThan(5)]);

        cache.Should().ContainKey(lookup);
        (lookup == cache.Keys.Single()).Should().BeTrue();
    }

    // --- Validation -----------------------------------------------------------

    [Fact]
    public void ParseFilters_InvalidFilter_ReturnsModelErrors()
    {
        QueryResult<FilterSet<TestField>> result = _model.ParseFilters("""[{ "field": "Stock", "operator": "Equals", "value": 1 }]""");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FieldNotFilterable);
    }

    [Fact]
    public void ParseFilters_MalformedJson_ReturnsFiltersMalformed()
    {
        QueryResult<FilterSet<TestField>> result = _model.ParseFilters("[{ not json");

        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FiltersMalformed);
    }

}
