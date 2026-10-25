namespace Qorel.EntityFramework.Tests.Pipeline;

/// <summary>
/// Runs the same filters in SQLite and in memory with <c>QueryModel.ApplyFilters</c> and checks they return the same
/// items. Nulls are the main thing that tends to differ between the two.
/// </summary>
public class InMemoryParityTests
{
    private static readonly DateTimeOffset _baseDate = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly ProductEntity[] _seed =
    [
        new() { Id = Guid.NewGuid(), Name = "Alpha Widget",  Price = 10.00m, Stock = 5,  Category = ProductCategory.Electronics, CreatedAt = _baseDate.AddDays(0), Rating = 4.5 },
        new() { Id = Guid.NewGuid(), Name = "Beta Gadget",   Price = 25.00m, Stock = 12, Category = ProductCategory.Electronics, CreatedAt = _baseDate.AddDays(1), Rating = 3.0 },
        new() { Id = Guid.NewGuid(), Name = "Gamma Shirt",   Price = 15.00m, Stock = 30, Category = ProductCategory.Clothing,    CreatedAt = _baseDate.AddDays(2), Rating = null },
        new() { Id = Guid.NewGuid(), Name = "Delta Jeans",   Price = 40.00m, Stock = 8,  Category = ProductCategory.Clothing,    CreatedAt = _baseDate.AddDays(3), Rating = 2.0 },
        new() { Id = Guid.NewGuid(), Name = "Epsilon Apple", Price = 5.00m,  Stock = 50, Category = ProductCategory.Food,        CreatedAt = _baseDate.AddDays(4), Rating = 5.0 },
        new() { Id = Guid.NewGuid(), Name = "Zeta Bread",    Price = 3.00m,  Stock = 20, Category = ProductCategory.Food,        CreatedAt = _baseDate.AddDays(5), Rating = null },
    ];

    public static TheoryData<string, Filter<ProductField>[]> Cases() => new()
    {
        { "Equals", [ProductFields.Name.EqualTo("Beta Gadget")] },
        { "Contains", [ProductFields.Name.Contains("a")] },
        { "StartsWith", [ProductFields.Name.StartsWith("Ze")] },
        { "EndsWith", [ProductFields.Name.EndsWith("Jeans")] },
        { "GreaterThan", [ProductFields.Price.GreaterThan(10m)] },
        { "GreaterThanOrEquals", [ProductFields.Price.GreaterThanOrEquals(10m)] },
        { "LessThan", [ProductFields.Stock.LessThan(12)] },
        { "LessThanOrEquals", [ProductFields.Stock.LessThanOrEquals(12)] },
        { "Date range", [ProductFields.CreatedAt.GreaterThan(_baseDate.AddDays(1)), ProductFields.CreatedAt.LessThanOrEquals(_baseDate.AddDays(4))] },
        { "Enum Equals", [ProductFields.Category.EqualTo(ProductCategory.Food)] },
        { "Enum IsOneOf", [ProductFields.Category.IsOneOf([ProductCategory.Food, ProductCategory.Clothing])] },
        { "IsNotOneOf", [ProductFields.Stock.IsNotOneOf([5, 30])] },
        { "Combined", [ProductFields.Category.EqualTo(ProductCategory.Electronics), ProductFields.Price.LessThan(20m)] },
        { "Nullable IsNull", [ProductFields.Rating.IsNull()] },
        { "Nullable IsNotNull", [ProductFields.Rating.IsNotNull()] },
        { "Nullable Equals value", [ProductFields.Rating.EqualTo(3.0)] },
        { "Nullable Equals null", [ProductFields.Rating.EqualTo((double?)null)] },
        { "Nullable IsOneOf with null", [ProductFields.Rating.IsOneOf([3.0, null])] },
        { "Nullable IsNotOneOf", [ProductFields.Rating.IsNotOneOf([3.0, 5.0])] },
        { "Nullable IsNotOneOf with null", [ProductFields.Rating.IsNotOneOf([3.0, null])] },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task InMemory_MatchesSql(string description, Filter<ProductField>[] filters)
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(_seed);

        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>()
            .Where(filters)
            .WithPageSize(100);

        QueryResult<Page<string>> sql = await fixture.Pipeline.QueryAsync(query, e => e.Name);
        IEnumerable<string> inMemory = new ProductQueryModel().ApplyFilters(_seed, filters).Select(e => e.Name);

        inMemory.Should().BeEquivalentTo(sql.Value.Items, description);
    }
}
