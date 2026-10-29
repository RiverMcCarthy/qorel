using Microsoft.EntityFrameworkCore;

using Qorel;
using Qorel.Samples.Data;
using Qorel.Samples.Products;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// If GetValueOrThrow() throws a QueryValidationException, this returns the same 422 that ToHttpResult() would.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<QueryValidationExceptionHandler>();

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("DataSource=sample.db"));

builder.Services.AddQorel(options =>
    {
        options.DefaultPageSize = 20;
        options.MaxPageSize = 100;
    })
    .AddModel(new ProductQueryModel())
    .AddEntityFramework<AppDbContext>();

WebApplication app = builder.Build();

// Recreate and seed the database each time the app starts. EnsureCreated won't update a table that already
// exists, so without this an old sample.db would keep its old schema.
using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();

    db.Products.AddRange(SeedData());
    db.SaveChanges();
}

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

// GET /products/fields - lists the fields clients can filter and sort on
app.MapGet("/products/fields", (ProductQueryModel model) => model.ClientFields.Select(field => new ProductFieldInfo(
    field.Field,
    field.IsFilterable,
    field.IsSortable,
    field.IsSearchable,
    [.. field.AllowedOperators.Order()])))
.WithName("GetProductFields")
.WithSummary("Returns the fields that clients can filter and sort on.");

// GET /products?filters=[...]&sort=price:desc&search=red&pageSize=20&cursor=... - same query as a GET so it can be cached
app.MapGet("/products", async (
    [AsParameters] QueryParameters<ProductField> query,
    IQueryPipeline<ProductEntity, ProductField> pipeline,
    CancellationToken cancellationToken) =>
{
    QueryResult<Page<ProductDto>> result = await pipeline.GetPageAsync(
        query,
        e => new ProductDto(e.Id, e.Name, e.Price, e.Category, e.Stock),
        cancellationToken: cancellationToken);

    return result.ToHttpResult();
})
.WithName("ListProducts")
.WithSummary("Filtered, sorted, searched, cursor-paginated products, from query string parameters.");

// POST /products/query - returns a page of products, validated, filtered and sorted
app.MapPost("/products/query", async (
    QueryRequest<ProductField> request,
    IQueryPipeline<ProductEntity, ProductField> pipeline,
    CancellationToken cancellationToken) =>
{
    QueryResult<Page<ProductDto>> result = await pipeline.GetPageAsync(
        request,
        e => new ProductDto(e.Id, e.Name, e.Price, e.Category, e.Stock),
        cancellationToken: cancellationToken);

    return result.ToHttpResult();
})
.WithName("QueryProducts")
.WithSummary("Filtered, sorted, cursor-paginated product query.");

// POST /products/count - number of matching products, e.g. for a total in the UI
app.MapPost("/products/count", async (
    QueryRequest<ProductField> request,
    IQueryPipeline<ProductEntity, ProductField> pipeline,
    CancellationToken cancellationToken) =>
{
    QueryResult<ItemCount> result = await pipeline.CountAsync(request, maxCount: 1000, cancellationToken: cancellationToken);

    return result.ToHttpResult();
})
.WithName("CountProducts")
.WithSummary("Counts the products matching a query, capped at 1000.");

// GET /products/{id} - gets a single product
app.MapGet("/products/{id:guid}", async (
    Guid id,
    IQueryPipeline<ProductEntity, ProductField> pipeline,
    CancellationToken cancellationToken) =>
{
    ProductDto? product = await pipeline.FindAsync(
        id,
        e => new ProductDto(e.Id, e.Name, e.Price, e.Category, e.Stock),
        cancellationToken: cancellationToken);

    return product is null ? Results.NotFound() : Results.Ok(product);
})
.Produces<ProductDto>()
.Produces(StatusCodes.Status404NotFound)
.WithName("GetProduct")
.WithSummary("Returns a single product by id.");

app.Run();

static IEnumerable<ProductEntity> SeedData() =>
[
    new() { Id = Guid.NewGuid(), Name = "Wireless Headphones",      Price = 79.99m,  Stock = 42,  Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow.AddDays(-30) },
    new() { Id = Guid.NewGuid(), Name = "Mechanical Keyboard",      Price = 129.99m, Stock = 15,  Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow.AddDays(-25) },
    new() { Id = Guid.NewGuid(), Name = "USB-C Hub",                Price = 34.99m,  Stock = 88,  Category = ProductCategory.Electronics, CreatedAt = DateTimeOffset.UtcNow.AddDays(-20) },
    new() { Id = Guid.NewGuid(), Name = "Running Shoes",            Price = 89.99m,  Stock = 30,  Category = ProductCategory.Clothing,    CreatedAt = DateTimeOffset.UtcNow.AddDays(-15) },
    new() { Id = Guid.NewGuid(), Name = "Merino Wool Sweater",      Price = 59.99m,  Stock = 12,  Category = ProductCategory.Clothing,    CreatedAt = DateTimeOffset.UtcNow.AddDays(-10) },
    new() { Id = Guid.NewGuid(), Name = "Organic Coffee Beans",     Price = 14.99m,  Stock = 200, Category = ProductCategory.Food,        CreatedAt = DateTimeOffset.UtcNow.AddDays(-5)  },
    new() { Id = Guid.NewGuid(), Name = "Dark Chocolate Bar",       Price = 4.99m,   Stock = 150, Category = ProductCategory.Food,        CreatedAt = DateTimeOffset.UtcNow.AddDays(-3)  },
    new() { Id = Guid.NewGuid(), Name = "Clean Code",               Price = 39.99m,  Stock = 25,  Category = ProductCategory.Books,       CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)  },
    new() { Id = Guid.NewGuid(), Name = "The Pragmatic Programmer", Price = 44.99m,  Stock = 18,  Category = ProductCategory.Books,       CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)  },
];
