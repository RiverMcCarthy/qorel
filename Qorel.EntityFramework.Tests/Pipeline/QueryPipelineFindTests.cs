namespace Qorel.EntityFramework.Tests.Pipeline;

public class QueryPipelineFindTests
{
    [Fact]
    public async Task FindAsync_ExistingId_ReturnsProjectedItem()
    {
        var id = Guid.NewGuid();
        IEnumerable<ProductEntity> seed =
        [
            new() { Id = id,           Name = "Target",    Price = 50m, Stock = 1, Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Other One", Price = 10m, Stock = 2, Category = ProductCategory.Food,        CreatedAt = DateTimeOffset.UtcNow },
        ];

        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(seed);

        string? name = await fixture.Pipeline.FindAsync<Guid, string>(id, e => e.Name);

        name.Should().Be("Target");
    }

    [Fact]
    public async Task FindAsync_NonExistentId_ReturnsNull()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync([]);

        string? name = await fixture.Pipeline.FindAsync<Guid, string>(Guid.NewGuid(), e => e.Name);

        name.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_ProjectsOnlyRequestedFields()
    {
        var id = Guid.NewGuid();
        IEnumerable<ProductEntity> seed =
        [
            new() { Id = id, Name = "Widget", Price = 99m, Stock = 5, Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow },
        ];

        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(seed);

        (Guid ResultId, string ResultName) = await fixture.Pipeline.FindAsync(
            id,
            e => ValueTuple.Create(e.Id, e.Name));

        ResultId.Should().Be(id);
        ResultName.Should().Be("Widget");
    }

    [Fact]
    public async Task FindAsync_ServerFilters_MustAlsoMatch()
    {
        var supplier = Guid.NewGuid();
        var item = new ProductEntity { Name = "Theirs", SupplierId = Guid.NewGuid() };
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync([item]);

        string? name = await fixture.Pipeline.FindAsync(item.Id, e => e.Name, [ProductFields.SupplierId.EqualTo(supplier)]);

        name.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_IdOfTheWrongType_Throws()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync([]);

        Func<Task> act = () => fixture.Pipeline.FindAsync(42, e => e.Name);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*'Id' is a Guid field, but the value is Int32*");
    }
}
