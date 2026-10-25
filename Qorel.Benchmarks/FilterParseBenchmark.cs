using System.Text.Json;

using BenchmarkDotNet.Attributes;

using Qorel.Benchmarks.Fixtures;

namespace Qorel.Benchmarks;

/// <summary>
/// Benchmarks <c>QueryModel.ParseFilters</c>. Every client request goes through it, so it needs to be fast.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class FilterParseBenchmark
{
    private readonly BenchmarkQueryModel _model = new();

    private FilterRequest<BenchmarkField>[] _oneFilter = null!;
    private FilterRequest<BenchmarkField>[] _fiveFilters = null!;

    [GlobalSetup]
    public void Setup()
    {
        _ = _model.ClientFields; // build the model before measuring

        _oneFilter =
        [
            Filter(BenchmarkField.Name, FilterOperator.Contains, "\"Pro\""),
        ];

        _fiveFilters =
        [
            Filter(BenchmarkField.Name, FilterOperator.Contains, "\"Pro\""),
            Filter(BenchmarkField.Price, FilterOperator.GreaterThan, "10.00"),
            Filter(BenchmarkField.Price, FilterOperator.LessThanOrEquals, "500.00"),
            Filter(BenchmarkField.Category, FilterOperator.Equals, "\"Electronics\""),
            Filter(BenchmarkField.Stock, FilterOperator.IsOneOf, "[1, 2, 3]"),
        ];
    }

    [Benchmark(Baseline = true, Description = "ParseFilters, 1 filter")]
    public QueryResult<FilterSet<BenchmarkField>> OneFilter() => _model.ParseFilters(_oneFilter);

    [Benchmark(Description = "ParseFilters, 5 filters")]
    public QueryResult<FilterSet<BenchmarkField>> FiveFilters() => _model.ParseFilters(_fiveFilters);

    private static FilterRequest<BenchmarkField> Filter(BenchmarkField field, FilterOperator op, string json)
        => new() { Field = field, Operator = op, Value = JsonDocument.Parse(json).RootElement };
}
