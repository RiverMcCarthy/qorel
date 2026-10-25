using System.Linq.Expressions;

using Microsoft.Extensions.DependencyInjection;

namespace Qorel.EntityFramework.Tests.Pipeline;

/// <summary>
/// Registered <see cref="IServerFilter{TEntity}"/>s should apply to every pipeline call, so no endpoint can miss them.
/// </summary>
public class ServerFilterTests
{
    private static readonly Guid _mine = Guid.NewGuid();
    private static readonly Guid _theirs = Guid.NewGuid();

    private static readonly ProductEntity[] _seed =
    [
        new() { Name = "Mine A", SupplierId = _mine },
        new() { Name = "Mine B", SupplierId = _mine, InStock = true },
        new() { Name = "Theirs", SupplierId = _theirs },
        new() { Name = "Shared", SupplierId = _theirs, Brand = "Shared" },
    ];

    // Fake current user, resolved per scope like it would be in a real request.
    private sealed record CurrentSupplier(Guid Id);

    // Something a field filter can't do: my items, or anything branded as shared.
    private sealed class VisibleProducts(CurrentSupplier supplier) : IServerFilter<ProductEntity>
    {
        public Expression<Func<ProductEntity, bool>> Predicate
            => product => product.SupplierId == supplier.Id || product.Brand == "Shared";
    }

    private sealed class InStockOnly : IServerFilter<ProductEntity>
    {
        public Expression<Func<ProductEntity, bool>> Predicate => product => product.InStock;
    }

    private static Task<PipelineFixture> CreateAsync(Action<IServiceCollection>? more = null)
        => PipelineFixture.CreateAsync(_seed, configureServices: services =>
        {
            services.AddScoped(_ => new CurrentSupplier(_mine));
            services.AddScoped<IServerFilter<ProductEntity>, VisibleProducts>();
            more?.Invoke(services);
        });

    [Fact]
    public async Task QueryAsync_OnlyReturnsVisibleItems()
    {
        await using PipelineFixture fixture = await CreateAsync();

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(new QueryRequest<ProductField>(), e => e.Name);

        result.Value.Items.Should().BeEquivalentTo("Mine A", "Mine B", "Shared");
    }

    [Fact]
    public async Task CountAsync_OnlyCountsVisibleItems()
    {
        await using PipelineFixture fixture = await CreateAsync();

        (await fixture.Pipeline.CountAsync(new QueryBuilder<ProductField>())).Value.Count.Should().Be(3);
    }

    [Fact]
    public async Task FindAsync_CannotReachOtherItems()
    {
        await using PipelineFixture fixture = await CreateAsync();

        Guid theirs = _seed.Single(product => product.Name == "Theirs").Id;

        (await fixture.Pipeline.FindAsync(theirs, e => e.Name)).Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_OnlyListsVisibleItems()
    {
        await using PipelineFixture fixture = await CreateAsync();

        (await fixture.Pipeline.ListAsync([], e => e.Name)).Should().BeEquivalentTo("Mine A", "Mine B", "Shared");
    }

    [Fact]
    public async Task SeveralFilters_MustAllMatch()
    {
        await using PipelineFixture fixture = await CreateAsync(services => services.AddScoped<IServerFilter<ProductEntity>, InStockOnly>());

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(new QueryRequest<ProductField>(), e => e.Name);

        result.Value.Items.Should().Equal("Mine B");
    }

    [Fact]
    public async Task FiltersPassedToACall_AreAddedToTheRegisteredOnes()
    {
        await using PipelineFixture fixture = await CreateAsync();

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(
            new QueryRequest<ProductField>(),
            e => e.Name,
            serverFilters: [ProductFields.Name.StartsWith("Mine")]);

        result.Value.Items.Should().BeEquivalentTo("Mine A", "Mine B");
    }

    [Fact]
    public async Task ServerFilterValues_AreParameters()
    {
        await using PipelineFixture fixture = await CreateAsync();

        await fixture.Pipeline.CountAsync(new QueryBuilder<ProductField>());

        fixture.Sql.Single().Should().NotContainEquivalentOf(_mine.ToString(), "the current supplier's id should be sent as a parameter");
    }
}
