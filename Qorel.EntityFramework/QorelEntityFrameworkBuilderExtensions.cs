using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Qorel;
using Qorel.EntityFramework;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers EF Core as the backend that runs Qorel queries.
/// </summary>
public static class QorelEntityFrameworkBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="IQueryPipeline{TEntity,TField}"/> for every model added with
    /// <see cref="QorelBuilder.AddModel{TEntity,TField}"/>, running queries with <typeparamref name="TDbContext"/>.
    /// </summary>
    /// <remarks>
    /// Register <typeparamref name="TDbContext"/> as usual, e.g. with <c>AddDbContext</c>. Pipelines are scoped, like the context.
    /// </remarks>
    /// <typeparam name="TDbContext">The app's context. Every model's entity must be part of it.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown when a backend is already registered.</exception>
    public static QorelBuilder AddEntityFramework<TDbContext>(this QorelBuilder builder)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Services.Any(service => service.ServiceType == typeof(DbContextAccessor)))
            throw new InvalidOperationException("A Qorel backend is already registered. Call AddEntityFramework once.");

        builder.Services.AddScoped(services => new DbContextAccessor(services.GetRequiredService<TDbContext>()));
        builder.Services.TryAddScoped(typeof(IQueryPipeline<,>), typeof(QueryPipeline<,>));
        return builder;
    }
}
