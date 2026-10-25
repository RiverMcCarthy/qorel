namespace Qorel.EntityFramework.Tests.Pipeline;

/// <summary>
/// Free-text search. Each word needs to match at least one searchable field.
/// </summary>
public class SearchTests
{
    private static readonly ProductEntity[] _seed =
    [
        new() { Name = "Red Running Shoes", Brand = "Stride" },
        new() { Name = "Blue Shoes", Brand = "Redwood" },
        new() { Name = "Red Scarf", Brand = null },
        new() { Name = "Green Hat", Brand = "Acme" },
    ];

    private static async Task<QueryResult<Page<string>>> SearchAsync(string? search, ProductQueryModel? model = null)
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(_seed, model ?? new ProductQueryModel(searchable: true));
        return await fixture.Pipeline.QueryAsync(new QueryRequest<ProductField> { Search = search }, e => e.Name);
    }

    [Fact]
    public async Task EveryWord_MustAppearInSomeSearchableField()
        => (await SearchAsync("red shoes")).Value.Items.Should().BeEquivalentTo("Red Running Shoes", "Blue Shoes");

    [Fact]
    public async Task Search_IgnoresCaseWhenTheFieldsDo()
        => (await SearchAsync("SCARF")).Value.Items.Should().Equal("Red Scarf");

    [Fact]
    public async Task Search_SkipsNullFields()
        => (await SearchAsync("acme")).Value.Items.Should().Equal("Green Hat");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NoWords_MeansNoSearch(string? search)
        => (await SearchAsync(search)).Value.Items.Should().HaveCount(4);

    [Fact]
    public async Task Search_CombinesWithFiltersAndCounts()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(_seed, new ProductQueryModel(searchable: true));

        QueryResult<ItemCount> count = await fixture.Pipeline.CountAsync(new QueryRequestBuilder<ProductField>()
            .Filter(ProductField.Name, FilterOperator.StartsWith, "Red")
            .WithSearch("shoes")
            .Build());

        count.Value.Count.Should().Be(1);
    }

    [Fact]
    public async Task Search_OnAModelWithoutSearchableFields_IsRejected()
        => (await SearchAsync("red", new ProductQueryModel()))
            .Errors.Should().ContainSingle(e => e.Code == ErrorCodes.SearchNotSupported);

    [Fact]
    public async Task Search_TooLong_IsRejected()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(
            _seed, new ProductQueryModel(searchable: true), configure: o => o.MaxSearchLength = 5);

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(new QueryRequest<ProductField> { Search = "longer than five" }, e => e.Name);

        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.SearchTooLong);
    }

    [Fact]
    public async Task Search_InCode_Works()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(_seed, new ProductQueryModel(searchable: true));

        QueryResult<Page<string>> result = await fixture.Pipeline.QueryAsync(new QueryBuilder<ProductField>().WithSearch("redwood"), e => e.Name);

        result.Value.Items.Should().Equal("Blue Shoes");
    }

    [Fact]
    public async Task Search_InCode_OnAModelWithoutSearchableFields_Throws()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(_seed);

        Func<Task> act = () => fixture.Pipeline.QueryAsync(new QueryBuilder<ProductField>().WithSearch("red"), e => e.Name);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no searchable fields*");
    }
}
