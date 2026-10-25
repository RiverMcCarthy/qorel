using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Qorel.EntityFramework.Tests.Fixtures;

/// <summary>
/// Sets up a new in-memory SQLite database and a pipeline, registered through DI the same way an app would.
/// </summary>
public sealed class PipelineFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;
    private readonly AsyncServiceScope _scope;
    private readonly List<string> _sql;

    private PipelineFixture(SqliteConnection connection, ServiceProvider services, List<string> sql)
    {
        _connection = connection;
        _services = services;
        _scope = services.CreateAsyncScope();
        _sql = sql;
    }

    public IQueryPipeline<ProductEntity, ProductField> Pipeline
        => _scope.ServiceProvider.GetRequiredService<IQueryPipeline<ProductEntity, ProductField>>();

    public ProductQueryModel Model => _scope.ServiceProvider.GetRequiredService<ProductQueryModel>();

    /// <summary>
    /// The SQL commands that have run so far, with their parameters.
    /// </summary>
    public IReadOnlyList<string> Sql => _sql;

    public static async Task<PipelineFixture> CreateAsync(
        IEnumerable<ProductEntity>? seed = null,
        ProductQueryModel? model = null,
        Action<QorelOptions>? configure = null,
        Action<IServiceCollection>? configureServices = null)
    {
        // The in-memory database is gone once this connection closes.
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        List<string> sql = [];
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => options
            .UseSqlite(connection)
            .LogTo(sql.Add, [RelationalEventId.CommandExecuted]));

        services.AddQorel(configure)
            .AddModel(model ?? new ProductQueryModel())
            .AddEntityFramework<TestDbContext>();

        configureServices?.Invoke(services);

        var fixture = new PipelineFixture(connection, services.BuildServiceProvider(validateScopes: true), sql);

        TestDbContext db = fixture._scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.Products.AddRange(seed ?? []);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        sql.Clear();

        return fixture;
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
