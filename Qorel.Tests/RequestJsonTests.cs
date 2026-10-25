using System.Text.Json;

using Qorel.Tests.Fixtures;

namespace Qorel.Tests;

/// <summary>
/// How filter and sort requests read from and write to JSON, whatever the app's serialiser options.
/// </summary>
public class RequestJsonTests
{
    [Fact]
    public void FilterRequest_RoundTrips()
    {
        var request = new FilterRequest<TestField>
        {
            Field = TestField.Price,
            Operator = FilterOperator.LessThan,
            Value = JsonSerializer.SerializeToElement(5),
        };

        string json = JsonSerializer.Serialize(request);

        json.Should().Be("""{"field":"Price","operator":"LessThan","value":5}""");
        JsonSerializer.Deserialize<FilterRequest<TestField>>(json)!.Field.Should().Be(TestField.Price);
    }

    [Fact]
    public void FilterRequest_WithoutValue_LeavesValueOut()
        => JsonSerializer.Serialize(new FilterRequest<TestField> { Field = TestField.Score, Operator = FilterOperator.IsNull })
            .Should().Be("""{"field":"Score","operator":"IsNull"}""");

    [Fact]
    public void FilterRequest_NamesIgnoreCase()
    {
        FilterRequest<TestField> request = JsonSerializer.Deserialize<FilterRequest<TestField>>("""{ "FIELD": "price", "Operator": "lessthan", "VALUE": 1 }""")!;

        request.Field.Should().Be(TestField.Price);
        request.Operator.Should().Be(FilterOperator.LessThan);
        request.Value.GetInt32().Should().Be(1);
    }

    [Fact]
    public void SortRequest_DirectionDefaultsToAsc()
        => JsonSerializer.Deserialize<SortRequest<TestField>>("""{ "field": "Price" }""")!.Direction.Should().Be(SortDirection.Asc);

    [Theory]
    [InlineData("""{ "field": "Price", "direction": "Sideways" }""")]
    [InlineData("""{ "field": "Price", "direction": 1 }""")]
    [InlineData("""{ "direction": "Asc" }""")]
    [InlineData("""[]""")]
    public void SortRequest_Malformed_Throws(string json)
    {
        Func<SortRequest<TestField>?> act = () => JsonSerializer.Deserialize<SortRequest<TestField>>(json);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void QueryRequest_ReadsWithWebDefaults()
    {
        QueryRequest<TestField> request = JsonSerializer.Deserialize<QueryRequest<TestField>>(
            """
            {
              "filters": [{ "field": "Price", "operator": "LessThan", "value": 5 }],
              "sorts": [{ "field": "Name", "direction": "Desc" }],
              "pageSize": 10,
              "cursor": "abc",
              "spatialBounds": { "minLatitude": 1, "minLongitude": 2, "maxLatitude": 3, "maxLongitude": 4 }
            }
            """,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        request.Filters.Should().ContainSingle().Which.Field.Should().Be(TestField.Price);
        request.Sorts.Should().ContainSingle().Which.Direction.Should().Be(SortDirection.Desc);
        request.PageSize.Should().Be(10);
        request.Cursor.Should().Be("abc");
        request.SpatialBounds.Should().Be(new BoundingBox(1, 2, 3, 4));
    }
}
