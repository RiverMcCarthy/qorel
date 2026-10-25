namespace Qorel.EntityFramework.Tests.Pipeline;

public class QueryPipelineFilterTests
{
    private static readonly DateTimeOffset _baseDate = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static IEnumerable<ProductEntity> SeedData() =>
    [
        new() { Id = Guid.NewGuid(), Name = "Alpha Widget",  Price = 10.00m, Stock = 5,  Category = ProductCategory.Electronics, CreatedAt = _baseDate.AddDays(0), Rating = 4.5 },
        new() { Id = Guid.NewGuid(), Name = "Beta Gadget",   Price = 25.00m, Stock = 12, Category = ProductCategory.Electronics, CreatedAt = _baseDate.AddDays(1), Rating = 3.0 },
        new() { Id = Guid.NewGuid(), Name = "Gamma Shirt",   Price = 15.00m, Stock = 30, Category = ProductCategory.Clothing,    CreatedAt = _baseDate.AddDays(2), Rating = null },
        new() { Id = Guid.NewGuid(), Name = "Delta Jeans",   Price = 40.00m, Stock = 8,  Category = ProductCategory.Clothing,    CreatedAt = _baseDate.AddDays(3), Rating = 2.0 },
        new() { Id = Guid.NewGuid(), Name = "Epsilon Apple", Price = 5.00m,  Stock = 50, Category = ProductCategory.Food,        CreatedAt = _baseDate.AddDays(4), Rating = 5.0 },
        new() { Id = Guid.NewGuid(), Name = "Zeta Bread",    Price = 3.00m,  Stock = 20, Category = ProductCategory.Food,        CreatedAt = _baseDate.AddDays(5), Rating = null },
    ];

    [Fact]
    public async Task Filter_Equals_ReturnsMatchingItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .Where(ProductFields.Name.EqualTo("Beta Gadget"));

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(query, e => e.Name);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle().Which.Should().Be("Beta Gadget");
    }

    [Fact]
    public async Task Filter_Contains_ReturnsMatchingItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .Where(ProductFields.Name.Contains("Gadget"));

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(query, e => e.Name);

        result.Value.Items.Should().ContainSingle().Which.Should().Contain("Gadget");
    }

    [Fact]
    public async Task Filter_StartsWith_ReturnsMatchingItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .Where(ProductFields.Name.StartsWith("Alpha"));

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(query, e => e.Name);

        result.Value.Items.Should().ContainSingle().Which.Should().StartWith("Alpha");
    }

    [Fact]
    public async Task Filter_EndsWith_ReturnsMatchingItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .Where(ProductFields.Name.EndsWith("Bread"));

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(query, e => e.Name);

        result.Value.Items.Should().ContainSingle().Which.Should().EndWith("Bread");
    }

    [Fact]
    public async Task Filter_IsOneOf_ReturnsAllMatchingItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .Where(ProductFields.Category.IsOneOf([ProductCategory.Food, ProductCategory.Clothing]));

        QueryResult<Page<ProductCategory>> result = await fixture.Pipeline.QueryAsync(query, e => e.Category);

        result.Value.Items.Should().HaveCount(4);
        result.Value.Items.Should().OnlyContain(c => c == ProductCategory.Food || c == ProductCategory.Clothing);
    }

    [Fact]
    public async Task Filter_IsNotOneOf_ExcludesMatchingItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .Where(ProductFields.Category.IsNotOneOf([ProductCategory.Electronics]));

        QueryResult<Page<ProductCategory>> result = await fixture.Pipeline.QueryAsync(query, e => e.Category);

        result.Value.Items.Should().HaveCount(4);
        result.Value.Items.Should().NotContain(ProductCategory.Electronics);
    }

    [Fact]
    public async Task Filter_GreaterThan_ReturnsItemsAboveThreshold()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .Where(ProductFields.Price.GreaterThan(20m));

        QueryResult<Page<decimal>> result = await fixture.Pipeline.QueryAsync(query, e => e.Price);

        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().OnlyContain(p => p > 20m);
    }

    [Fact]
    public async Task Filter_GreaterThanOrEquals_IncludesBoundaryValue()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .Where(ProductFields.Price.GreaterThanOrEquals(25m));

        QueryResult<Page<decimal>> result = await fixture.Pipeline.QueryAsync(query, e => e.Price);

        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().OnlyContain(p => p >= 25m);
    }

    [Fact]
    public async Task Filter_LessThan_ReturnsItemsBelowThreshold()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .Where(ProductFields.Price.LessThan(10m));

        QueryResult<Page<decimal>> result = await fixture.Pipeline.QueryAsync(query, e => e.Price);

        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().OnlyContain(p => p < 10m);
    }

    [Fact]
    public async Task Filter_LessThanOrEquals_IncludesBoundaryValue()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .Where(ProductFields.Price.LessThanOrEquals(10m));

        QueryResult<Page<decimal>> result = await fixture.Pipeline.QueryAsync(query, e => e.Price);

        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Should().OnlyContain(p => p <= 10m);
    }

    [Fact]
    public async Task Filter_IsNull_ReturnsOnlyNullRatingItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .Where(ProductFields.Rating.IsNull());

        QueryResult<Page<double?>> result = await fixture.Pipeline.QueryAsync(query, e => e.Rating);

        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().OnlyContain(r => r == null);
    }

    [Fact]
    public async Task Filter_IsNotNull_ReturnsOnlyNonNullRatingItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .Where(ProductFields.Rating.IsNotNull());

        QueryResult<Page<double?>> result = await fixture.Pipeline.QueryAsync(query, e => e.Rating);

        result.Value.Items.Should().HaveCount(4);
        result.Value.Items.Should().OnlyContain(r => r != null);
    }

    [Fact]
    public async Task Filter_MultipleFilters_AreAndedTogether()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .Where(
                ProductFields.Category.EqualTo(ProductCategory.Electronics),
                ProductFields.Price.LessThan(20m));

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(query, e => e.Name);

        result.Value.Items.Should().ContainSingle().Which.Should().Be("Alpha Widget");
    }

    [Fact]
    public async Task Filter_NoFilters_ReturnsAllItems()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(SeedData());

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(
            new QueryBuilder<ProductField>(),
            e => e.Name);

        result.Value.Count.Should().Be(6);
    }

    [Fact]
    public async Task Filter_EmptyDatabase_ReturnsEmptyPage()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync([]);

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(
            new QueryBuilder<ProductField>(),
            e => e.Name);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsEmpty.Should().BeTrue();
    }
}
