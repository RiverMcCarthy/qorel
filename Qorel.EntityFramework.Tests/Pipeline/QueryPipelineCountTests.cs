namespace Qorel.EntityFramework.Tests.Pipeline;

/// <summary>
/// Tests for <c>CountAsync</c>, with both a request and a <c>QueryBuilder</c>. The count should always match the
/// number of items you'd get by paging through the same query.
/// </summary>
public class QueryPipelineCountTests
{
    private static IEnumerable<ProductEntity> SeedData() =>
    [
        new() { Id = Guid.NewGuid(), Name = "Alpha", Price = 10m, Stock = 5,  Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Beta",  Price = 20m, Stock = 10, Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Gamma", Price = 30m, Stock = 15, Category = ProductCategory.Clothing,    CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Delta", Price = 40m, Stock = 20, Category = ProductCategory.Clothing,    CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Omega", Price = 50m, Stock = 25, Category = ProductCategory.Food,        CreatedAt = DateTimeOffset.UtcNow },
    ];

    // --- Request path ---------------------------------------------------------

    [Fact]
    public async Task Count_EmptyRequest_CountsAllItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryResult<ItemCount> result = await fixture.Pipeline.CountAsync(new QueryRequest<ProductField>());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new ItemCount(5));
    }

    [Fact]
    public async Task Count_WithFilters_CountsOnlyMatchingItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
            .Filter(ProductField.Category, FilterOperator.Equals, ProductCategory.Electronics)
            .Build();

        QueryResult<ItemCount> result = await fixture.Pipeline.CountAsync(request);

        result.Value.Count.Should().Be(2);
    }

    [Fact]
    public async Task Count_IgnoresSortsCursorAndPageSize()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryRequest<ProductField> firstPage = new QueryRequestBuilder<ProductField>()
            .Sort(ProductField.Stock, SortDirection.Desc)
            .WithPageSize(2)
            .Build();

        QueryResult<Page<string>> page = await fixture.Pipeline.QueryAsync(firstPage, e => e.Name);
        QueryRequest<ProductField> secondPage = firstPage with { Cursor = page.Value.NextCursor };

        QueryResult<ItemCount> result = await fixture.Pipeline.CountAsync(secondPage);

        result.Value.Count.Should().Be(5);
    }

    [Fact]
    public async Task Count_InvalidCursor_IsReportedLikeAnyOtherMistake()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryResult<ItemCount> result = await fixture.Pipeline.CountAsync(new QueryRequest<ProductField> { Cursor = "not-a-cursor" });

        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.CursorInvalid);
    }

    [Fact]
    public async Task Count_InvalidFilter_ReturnsValidationError()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
            .Filter((ProductField)999, FilterOperator.Equals, "x")
            .Build();

        QueryResult<ItemCount> result = await fixture.Pipeline.CountAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FieldUnknown);
    }

    [Fact]
    public async Task Count_ServerFilters_AreApplied()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
            .Filter(ProductField.Category, FilterOperator.Equals, ProductCategory.Electronics)
            .Build();

        QueryResult<ItemCount> result = await fixture.Pipeline.CountAsync(
            request,
            additionalFilters: [ProductFields.Price.LessThan(15m)]);

        result.Value.Count.Should().Be(1);
    }

    [Fact]
    public async Task Count_SpatialBoundsWithoutSpatialFilter_ReturnsSpatialNotSupported()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
            .WithSpatialBounds(BoundingBox.Create(0, 0, 1, 1))
            .Build();

        QueryResult<ItemCount> result = await fixture.Pipeline.CountAsync(request);

        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.SpatialNotSupported);
    }

    // --- Builder path ---------------------------------------------------------

    [Fact]
    public async Task Count_Builder_CountsMatchingItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .Where(ProductFields.Price.GreaterThanOrEquals(30m))
            .WithPageSize(1);

        QueryResult<ItemCount> result = await fixture.Pipeline.CountAsync(query);

        result.Value.Count.Should().Be(3);
    }

    [Fact]
    public async Task Count_EmptyDatabase_ReturnsZero()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync([]);

        QueryResult<ItemCount> result = await fixture.Pipeline.CountAsync(new QueryBuilder<ProductField>());

        result.Value.Should().Be(new ItemCount(0));
    }

    // --- Max count ------------------------------------------------------------

    [Fact]
    public async Task Count_MoreMatchesThanMaxCount_ReturnsCappedCount()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryResult<ItemCount> result = await fixture.Pipeline.CountAsync(
            new QueryBuilder<ProductField>(),
            maxCount: 3);

        result.Value.Should().Be(new ItemCount(3, IsCapped: true));
    }

    [Fact]
    public async Task Count_MatchesEqualToMaxCount_IsNotCapped()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryResult<ItemCount> result = await fixture.Pipeline.CountAsync(
            new QueryBuilder<ProductField>(),
            maxCount: 5);

        result.Value.Should().Be(new ItemCount(5));
    }

    [Fact]
    public async Task Count_FewerMatchesThanMaxCount_ReturnsExactCount()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryResult<ItemCount> result = await fixture.Pipeline.CountAsync(
            new QueryBuilder<ProductField>(),
            maxCount: 100);

        result.Value.Should().Be(new ItemCount(5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task Count_InvalidMaxCount_Throws(int maxCount)
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        Func<Task> act = () => fixture.Pipeline.CountAsync(new QueryBuilder<ProductField>(), maxCount: maxCount);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
