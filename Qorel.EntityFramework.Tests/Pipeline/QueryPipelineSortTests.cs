namespace Qorel.EntityFramework.Tests.Pipeline;

public class QueryPipelineSortTests
{
    private static IEnumerable<ProductEntity> SeedData() =>
    [
        new() { Id = Guid.NewGuid(), Name = "Charlie", Price = 30m, Stock = 30, Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Alice",   Price = 10m, Stock = 10, Category = ProductCategory.Clothing,    CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Bob",     Price = 20m, Stock = 20, Category = ProductCategory.Food,        CreatedAt = DateTimeOffset.UtcNow },
    ];

    [Fact]
    public async Task OrderBy_Ascending_ReturnsSortedItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .OrderBy(ProductFields.Name);

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(query, e => e.Name);

        result.Value.Items.Should().Equal("Alice", "Bob", "Charlie");
    }

    [Fact]
    public async Task OrderByDescending_ReturnsSortedItemsInReverseOrder()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .OrderByDescending(ProductFields.Stock);

        QueryResult<Page<int>> result = await fixture.Pipeline.QueryAsync(query, e => e.Stock);

        result.Value.Items.Should().Equal(30, 20, 10);
    }

    [Fact]
    public async Task ThenBy_AppliesSecondarySort()
    {
        IEnumerable<ProductEntity> seed =
        [
            new() { Id = Guid.NewGuid(), Name = "Zeta",  Price = 10m, Stock = 10, Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Alpha", Price = 10m, Stock = 10, Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Beta",  Price = 20m, Stock = 20, Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow },
        ];

        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(seed);

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .OrderBy(ProductFields.Stock)
            .ThenBy(ProductFields.Name);

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(query, e => e.Name);

        result.Value.Items.Should().Equal("Alpha", "Zeta", "Beta");
    }

    [Fact]
    public async Task ThenByDescending_AppliesSecondaryDescendingSort()
    {
        IEnumerable<ProductEntity> seed =
        [
            new() { Id = Guid.NewGuid(), Name = "Alpha", Price = 10m, Stock = 10, Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Zeta",  Price = 10m, Stock = 10, Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Beta",  Price = 20m, Stock = 20, Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow },
        ];

        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(seed);

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .OrderBy(ProductFields.Stock)
            .ThenByDescending(ProductFields.Name);

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(query, e => e.Name);

        result.Value.Items.Should().Equal("Zeta", "Alpha", "Beta");
    }
}
