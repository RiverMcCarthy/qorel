namespace Qorel.EntityFramework.Tests.Pipeline;

/// <summary>
/// Filter and cursor values should be sent as SQL parameters, not inlined as literals. Otherwise every new value means
/// EF Core compiles another query and the database caches another plan.
/// </summary>
public class SqlParameterTests
{
    private static IEnumerable<ProductEntity> Seed() =>
        Enumerable.Range(1, 6).Select(i => new ProductEntity { Name = $"Item {i}", Stock = i, Brand = i % 2 == 0 ? "Acme" : null });

    [Fact]
    public async Task FilterValues_AreParameters()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(Seed());

        await fixture.Pipeline.GetPageAsync(
            new QueryBuilder<ProductField>().Where(
                ProductFields.Stock.GreaterThan(4321),
                ProductFields.Name.Contains("needle"),
                ProductFields.Brand.IsOneOf("Acme", "Bolt")),
            e => e.Id);

        string sql = fixture.Sql.Single();
        sql.Should().NotContain("4321").And.NotContain("needle").And.NotContain("Bolt");
    }

    [Fact]
    public async Task DifferentValues_RunTheSameSql()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(Seed());

        await fixture.Pipeline.CountAsync(new QueryBuilder<ProductField>().Where(ProductFields.Stock.GreaterThan(1)));
        await fixture.Pipeline.CountAsync(new QueryBuilder<ProductField>().Where(ProductFields.Stock.GreaterThan(2)));

        fixture.Sql.Select(CommandText).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task CursorValues_AreParameters()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(Seed());
        QueryBuilder<ProductField> query = new QueryBuilder<ProductField>().OrderBy(ProductFields.Name).WithPageSize(2);

        QueryResult<Page<string>> first = await fixture.Pipeline.GetPageAsync(query, e => e.Name);
        await fixture.Pipeline.GetPageAsync(query.WithCursor(first.Value.NextCursor), e => e.Name);
        QueryResult<Page<string>> second = await fixture.Pipeline.GetPageAsync(query.WithCursor(first.Value.NextCursor), e => e.Name);
        await fixture.Pipeline.GetPageAsync(query.WithCursor(second.Value.NextCursor), e => e.Name);

        fixture.Sql.Last().Should().NotContain("Item 4");
        fixture.Sql.Skip(1).Select(CommandText).Distinct().Should().ContainSingle();
    }

    // Strips the parameter values (the bracketed part at the start) from the logged command.
    private static string CommandText(string logged) => logged[logged.IndexOf("SELECT", StringComparison.Ordinal)..];
}
