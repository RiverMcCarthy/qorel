namespace Qorel.EntityFramework.Tests.Pipeline;

/// <summary>
/// Tests for <c>ListAsync</c>, which takes filters only with no paging or sorting.
/// </summary>
public class QueryPipelineListTests
{
    private static IEnumerable<ProductEntity> SeedData() =>
    [
        new() { Id = Guid.NewGuid(), Name = "Alpha", Price = 10m, Stock = 5,  Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Beta",  Price = 20m, Stock = 10, Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "Gamma", Price = 30m, Stock = 15, Category = ProductCategory.Clothing,    CreatedAt = DateTimeOffset.UtcNow },
    ];

    [Fact]
    public async Task ListAsync_NoFilters_ReturnsAllItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        IReadOnlyList<string> names = await fixture.Pipeline.ListAsync(
            [],
            e => e.Name);

        names.Should().HaveCount(3);
    }

    [Fact]
    public async Task ListAsync_WithFilter_ReturnsOnlyMatchingItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        Filter<ProductField>[] filters = [ProductFields.Category.EqualTo(ProductCategory.Electronics)];

        IReadOnlyList<string> names = await fixture.Pipeline.ListAsync(filters, e => e.Name);

        names.Should().HaveCount(2);
        names.Should().Contain("Alpha").And.Contain("Beta");
    }

    [Fact]
    public async Task ListAsync_EmptyDatabase_ReturnsEmptyList()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync([]);

        IReadOnlyList<string> names = await fixture.Pipeline.ListAsync([], e => e.Name);

        names.Should().BeEmpty();
    }
}
