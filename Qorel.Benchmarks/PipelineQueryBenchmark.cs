using BenchmarkDotNet.Attributes;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Qorel.Benchmarks.Fixtures;

namespace Qorel.Benchmarks;

/// <summary>
/// Runs full queries through <see cref="IQueryPipeline{TEntity,TField}"/> against in-memory SQLite. The numbers cover
/// building the query, EF Core translating it, and reading the results back.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class PipelineQueryBenchmark
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _provider = null!;
    private Guid _seedId;
    private string _secondPageCursor = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<BenchmarkDbContext>(options => options.UseSqlite(_connection));
        services.AddQorel().AddModel(new BenchmarkQueryModel()).AddEntityFramework<BenchmarkDbContext>();
        _provider = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        BenchmarkDbContext db = scope.ServiceProvider.GetRequiredService<BenchmarkDbContext>();
        await db.Database.EnsureCreatedAsync();

        List<BenchmarkProduct> products = [.. Seed(200)];
        _seedId = products[0].Id;
        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        QueryResult<Page<string>> firstPage = await PipelineIn(scope).GetPageAsync(ByName(), e => e.Name);
        _secondPageCursor = firstPage.Value.NextCursor!;
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Benchmark(Baseline = true, Description = "GetPageAsync, first page, no filters")]
    public async Task<QueryResult<Page<string>>> FirstPage()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        return await PipelineIn(scope).GetPageAsync(ByName(), e => e.Name);
    }

    [Benchmark(Description = "GetPageAsync, next page from a cursor")]
    public async Task<QueryResult<Page<string>>> NextPage()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        return await PipelineIn(scope).GetPageAsync(ByName().WithCursor(_secondPageCursor), e => e.Name);
    }

    [Benchmark(Description = "GetPageAsync, first page, 3 filters")]
    public async Task<QueryResult<Page<string>>> WithFilters()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();

        return await PipelineIn(scope).GetPageAsync(
            new QueryBuilder<BenchmarkField>()
                .Where(
                    BenchmarkFields.Category.EqualTo(BenchmarkCategory.Electronics),
                    BenchmarkFields.Price.GreaterThan(50m),
                    BenchmarkFields.Stock.GreaterThan(0))
                .OrderByDescending(BenchmarkFields.Price)
                .WithPageSize(20),
            e => e.Name);
    }

    [Benchmark(Description = "FindAsync by id")]
    public async Task<string?> FindById()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        return await PipelineIn(scope).FindAsync(_seedId, e => e.Name);
    }

    private static QueryBuilder<BenchmarkField> ByName() => new QueryBuilder<BenchmarkField>().OrderBy(BenchmarkFields.Name).WithPageSize(20);

    private static IQueryPipeline<BenchmarkProduct, BenchmarkField> PipelineIn(AsyncServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<IQueryPipeline<BenchmarkProduct, BenchmarkField>>();

    private static IEnumerable<BenchmarkProduct> Seed(int count)
    {
        BenchmarkCategory[] categories = Enum.GetValues<BenchmarkCategory>();

        return Enumerable.Range(1, count).Select(i => new BenchmarkProduct
        {
            Id = Guid.NewGuid(),
            Name = $"Product {i:D4}",
            Price = (i % 50 * 10m) + 5m,
            Stock = i % 20,
            Category = categories[i % categories.Length],
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-i),
        });
    }
}
