using BenchmarkDotNet.Attributes;

using Qorel.Benchmarks.Fixtures;

namespace Qorel.Benchmarks;

/// <summary>
/// Checks how much memory building a <see cref="QueryBuilder{TField}"/> allocates.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class QueryBuilderBenchmark
{
    [Benchmark(Baseline = true, Description = "QueryBuilder, no conditions")]
    public QueryBuilder<BenchmarkField> EmptyQuery()
        => new QueryBuilder<BenchmarkField>().WithPageSize(20);

    [Benchmark(Description = "QueryBuilder, 3 filters and 2 sorts")]
    public QueryBuilder<BenchmarkField> FilteredQuery()
        => new QueryBuilder<BenchmarkField>()
            .Where(
                BenchmarkFields.Category.EqualTo(BenchmarkCategory.Electronics),
                BenchmarkFields.Price.GreaterThan(10m),
                BenchmarkFields.Stock.GreaterThan(0))
            .OrderByDescending(BenchmarkFields.Price)
            .ThenBy(BenchmarkFields.Name)
            .WithPageSize(20);
}
