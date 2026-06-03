using Qorel.EntityFramework.Mapping;
using Qorel.EntityFramework.Pipeline;
using Qorel.Querying;
using Qorel.Schema;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Qorel.EntityFramework.Extensions;

/// <summary>
/// Extension methods for registering Qorel Entity Framework services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="FieldMap{TEntity,TKey}"/> and the corresponding <see cref="IQueryPipeline{TEntity,TKey}"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The enum type identifying fields in the schema.</typeparam>
    /// <typeparam name="TFieldMap">The concrete <see cref="FieldMap{TEntity,TKey}"/> subclass.</typeparam>
    /// <typeparam name="TDbContext">
    /// The concrete <see cref="DbContext"/> registered in the application's DI container.
    /// </typeparam>
    public static IServiceCollection AddQorelPipeline<TEntity, TKey, TFieldMap, TDbContext>(
        this IServiceCollection services)
        where TEntity : class
        where TKey : Enum
        where TFieldMap : FieldMap<TEntity, TKey>
        where TDbContext : DbContext
    {
        // Register the concrete map as itself so the pipeline can resolve it directly,
        // and also as IFieldMap<,> for consumer use.
        services.TryAddSingleton<TFieldMap>();
        services.TryAddSingleton<IFieldMap<TEntity, TKey>>(sp => sp.GetRequiredService<TFieldMap>());

        services.TryAddScoped<IQueryPipeline<TEntity, TKey>>(sp => new QueryPipeline<TEntity, TKey>(
            sp.GetRequiredService<TFieldMap>(),
            sp.GetRequiredService<FieldSchema<TKey>>(),
            sp.GetRequiredService<PaginationOptions>(),
            sp.GetRequiredService<TDbContext>()));

        return services;
    }
}
