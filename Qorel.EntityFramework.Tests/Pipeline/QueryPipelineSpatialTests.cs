using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Qorel.EntityFramework.Tests.Pipeline;

/// <summary>
/// The model's spatial filter gets added to the query when the request has <c>SpatialBounds</c>.
/// </summary>
public class QueryPipelineSpatialTests
{
    public sealed class GeoEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public enum GeoField { Id, Name, Latitude, Longitude }

    private static class GeoFields
    {
        public static readonly Field<GeoField, Guid> Id = new(GeoField.Id);
        public static readonly TextField<GeoField> Name = new(GeoField.Name);
        public static readonly ComparableField<GeoField, double> Latitude = new(GeoField.Latitude);
        public static readonly ComparableField<GeoField, double> Longitude = new(GeoField.Longitude);
    }

    private sealed class GeoQueryModel : QueryModel<GeoEntity, GeoField>
    {
        protected override void Configure(QueryModelBuilder<GeoEntity, GeoField> model)
        {
            model.Id(GeoFields.Id, e => e.Id);
            model.Map(GeoFields.Name, e => e.Name).Filterable().Sortable();
            model.Map(GeoFields.Latitude, e => e.Latitude).Filterable().Sortable();
            model.Map(GeoFields.Longitude, e => e.Longitude).Filterable().Sortable();

            model.SpatialFilter(box => e =>
                e.Latitude >= box.MinLatitude && e.Latitude <= box.MaxLatitude &&
                e.Longitude >= box.MinLongitude && e.Longitude <= box.MaxLongitude);
        }
    }

    private sealed class GeoDbContext(DbContextOptions<GeoDbContext> options) : DbContext(options)
    {
        public DbSet<GeoEntity> Locations => Set<GeoEntity>();
    }

    private static async Task<IQueryPipeline<GeoEntity, GeoField>> CreatePipelineAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<GeoDbContext>(options => options.UseSqlite(connection));
        services.AddQorel().AddModel(new GeoQueryModel()).AddEntityFramework<GeoDbContext>();

        IServiceProvider provider = services.BuildServiceProvider().CreateScope().ServiceProvider;

        GeoDbContext db = provider.GetRequiredService<GeoDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.Locations.AddRange(GridSeed());
        await db.SaveChangesAsync();

        return provider.GetRequiredService<IQueryPipeline<GeoEntity, GeoField>>();
    }

    // 5x5 grid of points, lat 0-4 and lon 0-4.
    private static IEnumerable<GeoEntity> GridSeed() =>
        from lat in Enumerable.Range(0, 5)
        from lon in Enumerable.Range(0, 5)
        select new GeoEntity { Id = Guid.NewGuid(), Name = $"({lat},{lon})", Latitude = lat, Longitude = lon };

    [Fact]
    public async Task SpatialBounds_ExcludesEntitiesOutsideBox()
    {
        IQueryPipeline<GeoEntity, GeoField> pipeline = await CreatePipelineAsync();

        QueryResult<Page<string>> result = await pipeline.GetPageAsync(
            new QueryBuilder<GeoField>().WithSpatialBounds(BoundingBox.Create(1, 1, 2, 2)),
            e => e.Name);

        result.Value.Items.Should().BeEquivalentTo(["(1,1)", "(1,2)", "(2,1)", "(2,2)"]);
    }

    [Fact]
    public async Task SpatialBounds_NoBoundsSet_ReturnsAllItems()
    {
        IQueryPipeline<GeoEntity, GeoField> pipeline = await CreatePipelineAsync();

        QueryResult<Page<string>> result = await pipeline.GetPageAsync(new QueryBuilder<GeoField>().WithPageSize(50), e => e.Name);

        result.Value.Count.Should().Be(25);
    }

    [Fact]
    public async Task SpatialBounds_BoundaryValuesAreInclusive()
    {
        IQueryPipeline<GeoEntity, GeoField> pipeline = await CreatePipelineAsync();

        QueryResult<Page<string>> result = await pipeline.GetPageAsync(
            new QueryBuilder<GeoField>().WithSpatialBounds(BoundingBox.Create(0, 0, 0, 0)),
            e => e.Name);

        result.Value.Items.Should().ContainSingle().Which.Should().Be("(0,0)");
    }

    [Fact]
    public async Task SpatialBounds_WithFilters_AppliesBoth()
    {
        IQueryPipeline<GeoEntity, GeoField> pipeline = await CreatePipelineAsync();

        QueryResult<Page<string>> result = await pipeline.GetPageAsync(
            new QueryBuilder<GeoField>()
                .Where(GeoFields.Latitude.EqualTo(1))
                .WithSpatialBounds(BoundingBox.Create(0, 0, 2, 2)),
            e => e.Name);

        result.Value.Items.Should().BeEquivalentTo(["(1,0)", "(1,1)", "(1,2)"]);
    }

    [Fact]
    public async Task SpatialBounds_Count_MatchesItemsInBox()
    {
        IQueryPipeline<GeoEntity, GeoField> pipeline = await CreatePipelineAsync();

        QueryResult<ItemCount> result = await pipeline.CountAsync(
            new QueryRequestBuilder<GeoField>().WithSpatialBounds(BoundingBox.Create(1, 1, 2, 2)).Build());

        result.Value.Count.Should().Be(4);
    }

    [Theory]
    [InlineData(-91, 0, 10, 10)]          // minLat below -90
    [InlineData(10, 0, 5, 10)]            // minLat > maxLat
    [InlineData(10, 30, 50, 10)]          // minLng > maxLng
    [InlineData(double.NaN, 0, 10, 10)]   // NaN is never in range
    public async Task SpatialBounds_InvalidBoxFromClient_ReturnsSpatialBoundsInvalid(double minLat, double minLng, double maxLat, double maxLng)
    {
        IQueryPipeline<GeoEntity, GeoField> pipeline = await CreatePipelineAsync();

        // JSON deserialization goes through the primary constructor, so Create()'s checks don't run.
        QueryRequest<GeoField> request = new QueryRequestBuilder<GeoField>()
            .WithSpatialBounds(new BoundingBox(minLat, minLng, maxLat, maxLng))
            .Build();

        QueryResult<Page<string>> result = await pipeline.GetPageAsync(request, e => e.Name);

        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.SpatialBoundsInvalid);
    }

    [Fact]
    public async Task SpatialBounds_ModelWithoutSpatialFilter_ReturnsSpatialNotSupported()
    {
        await using PipelineFixture fixture = await PipelineFixture.CreateAsync([]);

        QueryResult<Page<string>> result = await fixture.Pipeline.GetPageAsync(
            new QueryBuilder<ProductField>().WithSpatialBounds(BoundingBox.Create(0, 0, 1, 1)),
            e => e.Name);

        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.SpatialNotSupported);
    }
}
