namespace Qorel.EntityFramework.Tests.Pipeline;

/// <summary>
/// Paging through results should return the same items as one big query, in the same order, with nothing skipped or
/// repeated. The data has duplicates and nulls on purpose since that's where keyset paging usually breaks.
/// </summary>
public class KeysetPagingTests
{
    private static readonly DateTimeOffset _baseDate = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly string?[] _brands = ["Acme", null, "zeta", "Acme", "Bolt", null, "acme"];

    private static List<ProductEntity> Seed() =>
    [
        .. Enumerable.Range(0, 23).Select(i => new ProductEntity
        {
            Name = $"Item {i % 6}",                  // repeats, so the id breaks ties
            Price = i % 4 * 2.5m,
            Stock = i % 5,
            CreatedAt = _baseDate.AddHours(i % 3),
            Brand = _brands[i % _brands.Length],     // nulls, and values differing only by case
            InStock = i % 2 == 0,
            Rating = i % 4 == 0 ? null : i % 3 * 1.5, // nullable value types, with repeats
            Featured = (i % 3) switch { 0 => null, 1 => true, _ => false },
        }),
    ];

    public static TheoryData<string, bool> SortCases()
    {
        var cases = new TheoryData<string, bool>();

        foreach (string field in new[] { "Name", "Price", "Stock", "CreatedAt", "Brand", "InStock", "Rating", "Featured", "Id" })
        {
            cases.Add(field, false);
            cases.Add(field, true);
        }

        return cases;
    }

    [Theory]
    [MemberData(nameof(SortCases))]
    public async Task PagingThroughASort_MatchesOneUnpagedQuery(string field, bool descending)
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(Seed());
        var sort = new SortRequest<ProductField> { Field = Enum.Parse<ProductField>(field), Direction = descending ? SortDirection.Desc : SortDirection.Asc };

        QueryResult<Page<Guid>> everything = await fixture.Pipeline.QueryAsync(
            new QueryRequest<ProductField> { Sorts = [sort], PageSize = 100 },
            e => e.Id);

        List<Guid> paged = [];
        string? cursor = null;
        do
        {
            QueryResult<Page<Guid>> page = await fixture.Pipeline.QueryAsync(
                new QueryRequest<ProductField> { Sorts = [sort], PageSize = 4, Cursor = cursor },
                e => e.Id);

            page.IsSuccess.Should().BeTrue(string.Join("; ", page.Errors.Select(e => e.Message)));
            paged.AddRange(page.Value.Items);
            cursor = page.Value.NextCursor;
        }
        while (cursor is not null);

        everything.Value.Items.Should().HaveCount(23);
        paged.Should().Equal(everything.Value.Items);
    }

    [Theory]
    [InlineData(SortDirection.Asc)]
    [InlineData(SortDirection.Desc)]
    public async Task NullableSort_PutsNullsLastAscendingAndFirstDescending(SortDirection direction)
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(Seed());

        QueryResult<Page<string?>> result = await fixture.Pipeline.QueryAsync(
            new QueryRequest<ProductField> { Sorts = [new() { Field = ProductField.Brand, Direction = direction }], PageSize = 100 },
            e => e.Brand);

        IEnumerable<string?> expected = direction == SortDirection.Asc
            ? result.Value.Items.Where(b => b is not null).Order(StringComparer.Ordinal).Append(null).Distinct()
            : new string?[] { null }.Concat(result.Value.Items.Where(b => b is not null).OrderDescending(StringComparer.Ordinal).Distinct());

        result.Value.Items.Distinct().Should().Equal(expected);
    }

    [Fact]
    public async Task NullableValueSort_PutsNullsLast()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(Seed());

        QueryResult<Page<double?>> result = await fixture.Pipeline.QueryAsync(
            new QueryBuilder<ProductField>().OrderBy(ProductFields.Rating).WithPageSize(100),
            e => e.Rating);

        IReadOnlyList<double?> ratings = result.Value.Items;
        ratings.TakeWhile(rating => rating is not null).Should().HaveCount(17).And.BeInAscendingOrder();
        ratings.Skip(17).Should().HaveCount(6).And.OnlyContain(rating => rating == null);
    }

    [Fact]
    public async Task SortsBuiltInCode_PageTheSameWay()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(Seed());

        List<Guid> paged = [];
        string? cursor = null;
        do
        {
            QueryResult<Page<Guid>> page = await fixture.Pipeline.QueryAsync(
                new QueryBuilder<ProductField>().OrderByDescending(ProductFields.Brand).ThenBy(ProductFields.Name).WithPageSize(5).WithCursor(cursor),
                e => e.Id);

            paged.AddRange(page.Value.Items);
            cursor = page.Value.NextCursor;
        }
        while (cursor is not null);

        paged.Should().HaveCount(23).And.OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Cursor_FromADifferentSort_IsRejected()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(Seed());

        QueryResult<Page<Guid>> byPrice = await fixture.Pipeline.QueryAsync(
            new QueryBuilder<ProductField>().OrderBy(ProductFields.Price).WithPageSize(2),
            e => e.Id);

        QueryResult<Page<Guid>> byStock = await fixture.Pipeline.QueryAsync(
            new QueryBuilder<ProductField>().OrderBy(ProductFields.Stock).WithPageSize(2).WithCursor(byPrice.Value.NextCursor),
            e => e.Id);

        byStock.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.CursorInvalid);
    }

    [Fact]
    public async Task Cursor_TooLong_IsRejected()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(Seed(), configure: o => o.MaxCursorLength = 10);

        QueryResult<Page<Guid>> result = await fixture.Pipeline.QueryAsync(
            new QueryRequest<ProductField> { Cursor = new string('a', 11) },
            e => e.Id);

        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.CursorInvalid);
    }

    [Fact]
    public async Task EmptyCursor_StartsAtTheFirstPage()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync(Seed());

        QueryResult<Page<Guid>> result = await fixture.Pipeline.QueryAsync(new QueryRequest<ProductField> { Cursor = "" }, e => e.Id);

        result.Value.Items.Should().HaveCount(20);
    }
}
