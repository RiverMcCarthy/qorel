namespace Qorel.EntityFramework.Tests.Pipeline;

public class QueryPipelinePaginationTests
{
    // Order by Stock (an int) since SQLite can't ORDER BY a decimal.
    private static IEnumerable<ProductEntity> TenProducts() =>
        Enumerable.Range(1, 10).Select(i => new ProductEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Product {i:D2}",
            Price = i * 10m,
            Stock = i,
            Category = ProductCategory.Electronics,
            CreatedAt = DateTimeOffset.UtcNow,
        });

    [Fact]
    public async Task PageSize_LimitsReturnedItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(TenProducts());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .WithPageSize(3)
            .OrderBy(ProductFields.Stock);

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(query, e => e.Name);

        result.Value.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task HasNextPage_WhenMoreItemsExist_IsTrue()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(TenProducts());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .WithPageSize(3)
            .OrderBy(ProductFields.Stock);

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(query, e => e.Name);

        result.Value.HasNextPage.Should().BeTrue();
        result.Value.NextCursor.Should().NotBeNull();
    }

    [Fact]
    public async Task HasNextPage_OnLastPage_IsFalse()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(TenProducts());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .WithPageSize(10)
            .OrderBy(ProductFields.Stock);

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(query, e => e.Name);

        result.Value.HasNextPage.Should().BeFalse();
        result.Value.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task CursorPagination_ReturnsAllItemsWithNoGapsOrDuplicates()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(TenProducts());

        var allNames = new List<string>();
        string? cursor = null;

        do
        {
            QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
                .WithPageSize(3)
                .WithCursor(cursor)
                .OrderBy(ProductFields.Stock);

            QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(query, e => e.Name);
            result.IsSuccess.Should().BeTrue();

            allNames.AddRange(result.Value.Items);
            cursor = result.Value.NextCursor;
        } while (cursor is not null);

        allNames.Should().HaveCount(10);
        allNames.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task CursorPagination_PreservesOrderAcrossPages()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(TenProducts());

        var allStock = new List<int>();
        string? cursor = null;

        do
        {
            QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
                .WithPageSize(3)
                .WithCursor(cursor)
                .OrderBy(ProductFields.Stock);

            QueryResult<Page<int>> result = await fixture.Pipeline.QueryAsync(query, e => e.Stock);
            allStock.AddRange(result.Value.Items);
            cursor = result.Value.NextCursor;
        } while (cursor is not null);

        allStock.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task CursorPagination_DescendingOrder_ReturnsItemsInReverseOrder()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(TenProducts());

        var allStock = new List<int>();
        string? cursor = null;

        do
        {
            QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
                .WithPageSize(4)
                .WithCursor(cursor)
                .OrderByDescending(ProductFields.Stock);

            QueryResult<Page<int>> result = await fixture.Pipeline.QueryAsync(query, e => e.Stock);
            allStock.AddRange(result.Value.Items);
            cursor = result.Value.NextCursor;
        } while (cursor is not null);

        allStock.Should().HaveCount(10);
        allStock.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task InvalidCursor_ReturnsFailureResult()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(TenProducts());

        QueryRequest<ProductField> request = new()
        {
            Cursor = "not-a-valid-cursor",
            PageSize = 5,
            Sorts = [],
            Filters = [],
        };

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(request, e => e.Name);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.CursorInvalid);
    }
}
