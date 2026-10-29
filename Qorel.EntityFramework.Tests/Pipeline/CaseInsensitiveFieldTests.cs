namespace Qorel.EntityFramework.Tests.Pipeline;

/// <summary>
/// Text fields mapped with <c>IgnoreCase()</c> should match regardless of case, in SQL and in memory. SQLite is useful
/// here because by default some of its operators are case-sensitive and some aren't.
/// </summary>
public class CaseInsensitiveFieldTests
{
    private static readonly ProductEntity[] _seed =
    [
        new() { Id = Guid.NewGuid(), Name = "Alpha Widget", Price = 10m, Stock = 1, Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "beta gadget",  Price = 20m, Stock = 2, Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = Guid.NewGuid(), Name = "GAMMA SHIRT",  Price = 30m, Stock = 3, Category = ProductCategory.Clothing,    CreatedAt = DateTimeOffset.UtcNow },
    ];

    public static TheoryData<string, Filter<ProductField>, string[]> Cases() => new()
    {
        { "Equals", ProductFields.Name.EqualTo("ALPHA widget"), ["Alpha Widget"] },
        { "IsOneOf", ProductFields.Name.IsOneOf("Beta Gadget", "gamma shirt"), ["beta gadget", "GAMMA SHIRT"] },
        { "IsNotOneOf", ProductFields.Name.IsNotOneOf("alpha WIDGET"), ["beta gadget", "GAMMA SHIRT"] },
        { "Contains", ProductFields.Name.Contains("Widget"), ["Alpha Widget"] },
        { "Contains mixed", ProductFields.Name.Contains("MmA sH"), ["GAMMA SHIRT"] },
        { "StartsWith", ProductFields.Name.StartsWith("BETA"), ["beta gadget"] },
        { "EndsWith", ProductFields.Name.EndsWith("shirt"), ["GAMMA SHIRT"] },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Filter_IgnoresCase_InSqlAndInMemory(string description, Filter<ProductField> filter, string[] expected)
    {
        var model = new ProductQueryModel(ignoreCase: true);
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(_seed, model);

        QueryResult<Page<string>> sql = await fixture.Pipeline.GetPageAsync(
            new QueryBuilder<ProductField>().Where(filter),
            e => e.Name);
        IEnumerable<string> inMemory = model.ApplyFilters(_seed, [filter]).Select(e => e.Name);

        sql.Value.Items.Should().BeEquivalentTo(expected, description);
        inMemory.Should().BeEquivalentTo(expected, description);
    }

    [Fact]
    public async Task Filter_DefaultMapping_StaysCaseSensitive()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(_seed);

        QueryResult<Page<string>> result = await fixture.Pipeline.GetPageAsync(
            new QueryBuilder<ProductField>().Where(ProductFields.Name.EqualTo("alpha widget")),
            e => e.Name);

        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Sort_IsNotAffected()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(_seed, new ProductQueryModel(ignoreCase: true));

        QueryResult<Page<string>> result = await fixture.Pipeline.GetPageAsync(
            new QueryBuilder<ProductField>().OrderBy(ProductFields.Name),
            e => e.Name);

        // SQLite sorts by byte value so uppercase comes first. If we lowercased, "Alpha" would come first instead.
        result.Value.Items.Should().Equal("Alpha Widget", "GAMMA SHIRT", "beta gadget");
    }
}
