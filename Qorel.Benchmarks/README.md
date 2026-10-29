# Qorel.Benchmarks

BenchmarkDotNet benchmarks for the Qorel pipeline.

## Running

```bash
dotnet run -c Release
```

Always use `-c Release` - Debug builds are not representative.

## Running a specific benchmark

```bash
dotnet run -c Release --filter "*Filter*"
dotnet run -c Release --filter "*Pipeline*"
```

## Benchmarks

| Class | What it measures |
|---|---|
| `SchemaInitBenchmark` | One-time lazy init cost of `FieldSchema.EnsureInitialised()` |
| `FilterParseBenchmark` | `schema.ParseFilters()` with 1 and 5 filters |
| `QueryBuilderBenchmark` | Fluent `QueryBuilder` chain allocation |
| `PipelineQueryBenchmark` | Full `GetPageAsync` / `FindAsync` against SQLite in-memory |
