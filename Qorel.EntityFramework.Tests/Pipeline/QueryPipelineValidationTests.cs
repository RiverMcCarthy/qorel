namespace Qorel.EntityFramework.Tests.Pipeline;

/// <summary>
/// Requests get checked against the model before anything hits the database. Bad input should come back as validation
/// errors, not exceptions.
/// </summary>
public class QueryPipelineValidationTests
{
    private static async Task<QueryResult<Page<string>>> QueryAsync(QueryRequest<ProductField> request, Action<QorelOptions>? configure = null)
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync([], configure: configure);
        return await fixture.Pipeline.QueryAsync(request, e => e.Name);
    }

    // --- Filters --------------------------------------------------------------

    [Fact]
    public async Task Filter_UnknownField_ReturnsFieldUnknown()
    {
        QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
            .Filter((ProductField)999, FilterOperator.Equals, "x")
            .Build();

        (await QueryAsync(request)).Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FieldUnknown);
    }

    [Fact]
    public async Task Filter_ServerOnlyField_LooksUnknown()
    {
        QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
            .Filter(ProductField.SupplierId, FilterOperator.Equals, Guid.NewGuid())
            .Build();

        (await QueryAsync(request)).Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FieldUnknown);
    }

    [Fact]
    public async Task Filter_NotFilterable_ReturnsFieldNotFilterable()
    {
        // Id can be sorted on but not filtered.
        QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
            .Filter(ProductField.Id, FilterOperator.Equals, Guid.NewGuid())
            .Build();

        (await QueryAsync(request)).Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FieldNotFilterable);
    }

    [Fact]
    public async Task Filter_OperatorNotAllowed_ReturnsOperatorNotAllowed()
    {
        QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
            .Filter(ProductField.Name, FilterOperator.GreaterThan, "alpha")
            .Build();

        (await QueryAsync(request)).Errors.Should().ContainSingle(e => e.Code == ErrorCodes.OperatorNotAllowed);
    }

    [Fact]
    public async Task Filter_WrongValueType_ReturnsFilterValueInvalid()
    {
        QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
            .Filter(ProductField.Price, FilterOperator.Equals, "not a number")
            .Build();

        (await QueryAsync(request)).Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FilterValueInvalid && e.Field == "Price");
    }

    [Fact]
    public async Task Filter_UnknownEnumName_ReturnsFilterValueInvalid()
    {
        QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
            .Filter(ProductField.Category, FilterOperator.Equals, "Furniture")
            .Build();

        (await QueryAsync(request)).Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FilterValueInvalid);
    }

    [Fact]
    public async Task Filter_IsOneOfWithoutArray_ReturnsFilterValueInvalid()
    {
        QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
            .Filter(ProductField.Stock, FilterOperator.IsOneOf, 5)
            .Build();

        (await QueryAsync(request)).Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FilterValueInvalid);
    }

    [Fact]
    public async Task Filter_NullEntry_ReturnsFiltersMalformed()
    {
        var request = new QueryRequest<ProductField> { Filters = [null!] };

        (await QueryAsync(request)).Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FiltersMalformed);
    }

    [Fact]
    public async Task Filter_TooMany_ReturnsFiltersTooMany()
    {
        QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
            .Filter(ProductField.Stock, FilterOperator.GreaterThan, 1)
            .Filter(ProductField.Stock, FilterOperator.LessThan, 9)
            .Build();

        (await QueryAsync(request, o => o.MaxFilters = 1)).Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FiltersTooMany);
    }

    [Fact]
    public async Task Filter_TooManyValues_ReturnsFilterTooManyValues()
    {
        QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
            .FilterList(ProductField.Stock, FilterOperator.IsOneOf, Enumerable.Range(0, 3))
            .Build();

        (await QueryAsync(request, o => o.MaxFilterValues = 2)).Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FilterTooManyValues);
    }

    // --- Sorts ----------------------------------------------------------------

    [Fact]
    public async Task Sort_UnknownField_ReturnsFieldUnknown()
        => (await QueryAsync(new QueryRequestBuilder<ProductField>().Sort((ProductField)999).Build()))
            .Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FieldUnknown);

    [Fact]
    public async Task Sort_NotSortable_ReturnsFieldNotSortable()
        => (await QueryAsync(new QueryRequestBuilder<ProductField>().Sort(ProductField.Category).Build()))
            .Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FieldNotSortable);

    [Fact]
    public async Task Sort_SameFieldTwice_ReturnsSortDuplicate()
        => (await QueryAsync(new QueryRequestBuilder<ProductField>().Sort(ProductField.Price).Sort(ProductField.Price, SortDirection.Desc).Build()))
            .Errors.Should().ContainSingle(e => e.Code == ErrorCodes.SortDuplicate);

    [Fact]
    public async Task Sort_TooMany_ReturnsSortsTooMany()
        => (await QueryAsync(new QueryRequestBuilder<ProductField>().Sort(ProductField.Price).Sort(ProductField.Stock).Build(), o => o.MaxSorts = 1))
            .Errors.Should().ContainSingle(e => e.Code == ErrorCodes.SortsTooMany);

    [Fact]
    public async Task Sort_NullEntry_ReturnsSortsMalformed()
        => (await QueryAsync(new QueryRequest<ProductField> { Sorts = [null!] }))
            .Errors.Should().ContainSingle(e => e.Code == ErrorCodes.SortsMalformed);

    // --- The request as a whole -----------------------------------------------

    [Fact]
    public async Task EveryError_IsReturnedAtOnce()
    {
        QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
            .Filter((ProductField)999, FilterOperator.Equals, "x")
            .Filter(ProductField.Id, FilterOperator.Equals, Guid.NewGuid())
            .Sort(ProductField.Category)
            .Build();

        (await QueryAsync(request)).Errors.Should().HaveCount(3);
    }

    [Fact]
    public async Task NullLists_AreTreatedAsEmpty()
        => (await QueryAsync(new QueryRequest<ProductField> { Filters = null!, Sorts = null! })).IsSuccess.Should().BeTrue();

    [Fact]
    public async Task PageSize_AboveMaximum_IsLowered()
    {
        IEnumerable<ProductEntity> seed = Enumerable.Range(1, 5).Select(i => new ProductEntity { Name = $"P{i}" });
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(seed, configure: o => (o.DefaultPageSize, o.MaxPageSize) = (2, 3));

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(new QueryRequest<ProductField> { PageSize = 9999 }, e => e.Name);

        result.Value.Items.Should().HaveCount(3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task PageSize_MissingOrNotPositive_UsesTheDefault(int? pageSize)
    {
        IEnumerable<ProductEntity> seed = Enumerable.Range(1, 5).Select(i => new ProductEntity { Name = $"P{i}" });
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(seed, configure: o => o.DefaultPageSize = 2);

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(new QueryRequest<ProductField> { PageSize = pageSize }, e => e.Name);

        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task PageSizes_CanBeSetPerModel()
    {
        IEnumerable<ProductEntity> seed = Enumerable.Range(1, 6).Select(i => new ProductEntity { Name = $"P{i}" });
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(seed, new ProductQueryModel(configure: model => model.PageSize(defaultSize: 2, maxSize: 4)));

        QueryResult<Page<string>> byDefault = await fixture.Pipeline.QueryAsync(new QueryRequest<ProductField>(), e => e.Name);
        QueryResult<Page<string>> overMaximum = await fixture.Pipeline.QueryAsync(new QueryRequest<ProductField> { PageSize = 100 }, e => e.Name);
        QueryResult<Page<string>> inCode = await fixture.Pipeline.QueryAsync(new QueryBuilder<ProductField>(), e => e.Name);

        byDefault.Value.Items.Should().HaveCount(2);
        overMaximum.Value.Items.Should().HaveCount(4);
        inCode.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ValidRequest_Succeeds()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync([new ProductEntity { Name = "Widget" }]);

        QueryRequest<ProductField> request = new QueryRequestBuilder<ProductField>()
            .Filter(ProductField.Name, FilterOperator.Equals, "Widget")
            .Filter(ProductField.Category, FilterOperator.Equals, ProductCategory.Electronics)
            .Sort(ProductField.Name)
            .WithPageSize(10)
            .Build();

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(request, e => e.Name);

        result.Value.Items.Should().ContainSingle().Which.Should().Be("Widget");
    }
}
