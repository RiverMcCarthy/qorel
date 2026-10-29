# Testing

## Faking the pipeline

Handlers depend on `IQueryPipeline<TEntity, TField>`, which is in the core `Qorel` package, so you can unit test them with a fake and no database:

```csharp
// NSubstitute
var pipeline = Substitute.For<IQueryPipeline<Product, ProductField>>();
pipeline
    .GetPageAsync(Arg.Any<QueryRequest<ProductField>>(), Arg.Any<Expression<Func<Product, ProductDto>>>(), Arg.Any<IEnumerable<Filter<ProductField>>?>(), Arg.Any<CancellationToken>())
    .Returns(QueryResult<Page<ProductDto>>.Success(new Page<ProductDto> { Items = [dto] }));
```

For integration tests, run the real pipeline against in-memory SQLite or a throwaway Postgres container. Register it the same way your app does, with `AddQorel().AddModel(...).AddEntityFramework<TDbContext>()`. With in-memory SQLite, keep the connection open for the whole test or the database goes away.

You don't need DI to test a model. `new ProductQueryModel().ParseFilters(json)` validates client filters and `ApplyFilters` runs them in memory.

Server filters are just services, so register them in the test's services along with anything they depend on, like a fake current user. You can also test a filter on its own by compiling its `Predicate` and running it against a few items.

## Qorel.Testing

Builds client requests so you don't have to write the JSON by hand. Enums are sent by name, the same as a real client would.

```csharp
QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
    .Filter(ProductField.Price, FilterOperator.LessThan, 100m)
    .FilterList(ProductField.Category, FilterOperator.IsOneOf, [ProductCategory.Food, ProductCategory.Books])
    .FilterNull(ProductField.Rating, FilterOperator.IsNull)
    .Sort(ProductField.Price, SortDirection.Desc)
    .WithSearch("red")
    .WithPageSize(10)
    .Build();
```

`FilterRequestFactory` does the same for a single `FilterRequest`.

## Qorel.Testing.Assertions

Fluent assertions for results and pages, built on AwesomeAssertions:

```csharp
using Qorel.Testing.Assertions;

result.Should().Succeed();
result.Should().FailWithCode(ErrorCodes.FieldUnknown);
result.Should().SucceedWith().Which.Items.Should().HaveCount(10);
page.Should().HaveCount(10).And.HaveNextPage();
```
