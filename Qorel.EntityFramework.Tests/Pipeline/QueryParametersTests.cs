namespace Qorel.EntityFramework.Tests.Pipeline;

/// <summary>
/// Queries that come in as query string parameters, like on a GET endpoint.
/// </summary>
public class QueryParametersTests
{
    private static IEnumerable<ProductEntity> Seed() =>
    [
        new() { Name = "Alpha", Price = 10m, Category = ProductCategory.Food },
        new() { Name = "Bravo", Price = 30m, Category = ProductCategory.Food },
        new() { Name = "Charlie", Price = 20m, Category = ProductCategory.Clothing },
    ];

    [Fact]
    public async Task Parameters_RunLikeARequest()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(Seed());

        var parameters = new QueryParameters<ProductField>
        {
            Filters = """[{ "field": "Category", "operator": "Equals", "value": "Food" }]""",
            Sort = "price:desc, name",
            PageSize = 1,
        };

        QueryResult<Page<string>> first = await fixture.Pipeline.GetPageAsync(parameters, e => e.Name);
        QueryResult<Page<string>> second = await fixture.Pipeline.GetPageAsync(
            new QueryParameters<ProductField> { Filters = parameters.Filters, Sort = parameters.Sort, PageSize = 1, Cursor = first.Value.NextCursor },
            e => e.Name);

        first.Value.Items.Should().Equal("Bravo");
        second.Value.Items.Should().Equal("Alpha");
    }

    [Fact]
    public async Task Parameters_CanBeCounted()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(Seed());

        QueryResult<ItemCount> count = await fixture.Pipeline.CountAsync(new QueryParameters<ProductField> { Filters = """[{ "field": "Price", "operator": "GreaterThan", "value": 15 }]""" });

        count.Value.Count.Should().Be(2);
    }

    [Theory]
    [InlineData("[{ not json", null, null, ErrorCodes.FiltersMalformed)]
    [InlineData(null, "price:sideways", null, ErrorCodes.SortsMalformed)]
    [InlineData(null, "price:desc:extra", null, ErrorCodes.SortsMalformed)]
    [InlineData(null, "colour", null, ErrorCodes.FieldUnknown)]
    [InlineData(null, null, "1,2,3", ErrorCodes.SpatialBoundsInvalid)]
    public async Task UnreadableParameters_AreValidationErrors(string? filters, string? sort, string? bounds, string expectedCode)
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync([]);

        QueryResult<Page<string>> result = await fixture.Pipeline.GetPageAsync(
            new QueryParameters<ProductField> { Filters = filters, Sort = sort, Bounds = bounds },
            e => e.Name);

        result.Errors.Should().ContainSingle(e => e.Code == expectedCode);
    }

    [Fact]
    public async Task UnknownSortField_NamesTheField()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync([]);

        QueryResult<Page<string>> result = await fixture.Pipeline.GetPageAsync(new QueryParameters<ProductField> { Sort = "colour" }, e => e.Name);

        result.Errors.Single().Field.Should().Be("colour");
    }

    [Fact]
    public void Bounds_AreReadInvariantly()
        => new QueryParameters<ProductField> { Bounds = "51.4, -0.2, 51.6, 0.1" }.ToRequest()
            .Value.SpatialBounds.Should().Be(new BoundingBox(51.4, -0.2, 51.6, 0.1));
}
