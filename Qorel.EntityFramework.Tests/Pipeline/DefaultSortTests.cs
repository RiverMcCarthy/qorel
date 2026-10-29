namespace Qorel.EntityFramework.Tests.Pipeline;

/// <summary>
/// The model's default sort is used when a query doesn't specify one.
/// </summary>
public class DefaultSortTests
{
    private static IEnumerable<ProductEntity> Seed() =>
        Enumerable.Range(1, 7).Select(i => new ProductEntity { Name = $"Item {i}", Stock = i % 3, Rating = i == 4 ? null : i });

    // Stock 2, then 1, then 0, with the highest rating first within each. Descending puts the null rating (Item 4)
    // ahead of the rest.
    private static ProductQueryModel ByStockThenRatingDescending() => new(configure: model => model
        .DefaultSort(ProductFields.Stock, SortDirection.Desc)
        .DefaultSort(ProductFields.Rating, SortDirection.Desc));

    [Fact]
    public async Task NoSorts_UsesTheDefaultSort()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(Seed(), ByStockThenRatingDescending());

        QueryResult<Page<string>> result = await fixture.Pipeline.GetPageAsync(new QueryRequest<ProductField>(), e => e.Name);

        result.Value.Items.Should().Equal("Item 5", "Item 2", "Item 4", "Item 7", "Item 1", "Item 6", "Item 3");
    }

    [Fact]
    public async Task RequestedSorts_ReplaceTheDefault()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(Seed(), ByStockThenRatingDescending());

        QueryResult<Page<string>> result = await fixture.Pipeline.GetPageAsync(
            new QueryBuilder<ProductField>().OrderBy(ProductFields.Name),
            e => e.Name);

        result.Value.Items.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task DefaultSort_PagesLikeAnyOther()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(Seed(), ByStockThenRatingDescending());

        List<string> paged = [];
        string? cursor = null;
        do
        {
            QueryResult<Page<string>> page = await fixture.Pipeline.GetPageAsync(new QueryRequest<ProductField> { PageSize = 2, Cursor = cursor }, e => e.Name);
            paged.AddRange(page.Value.Items);
            cursor = page.Value.NextCursor;
        }
        while (cursor is not null);

        paged.Should().Equal("Item 5", "Item 2", "Item 4", "Item 7", "Item 1", "Item 6", "Item 3");
    }
}
