using Qorel.Querying;
using Qorel.Schema;

using Microsoft.Extensions.DependencyInjection;

namespace Qorel;

/// <summary>
/// Extension methods for registering Qorel services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers core Qorel services. Call once at application startup.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional delegate to override default pagination limits.</param>
    public static IServiceCollection AddQorel(
        this IServiceCollection services,
        Action<QorelOptions>? configure = null)
    {
        var options = new QorelOptions();
        configure?.Invoke(options);
        options.Validate();

        services.AddSingleton(new PaginationOptions
        {
            Default = options.DefaultPageSize,
            Maximum = options.MaximumPageSize,
        });

        return services;
    }

    /// <summary>
    /// Registers the field schema used by Qorel to resolve and validate queryable fields for a specific field key enum.
    /// </summary>
    /// <typeparam name="TSchema">
    /// The concrete <see cref="FieldSchema{TKey}"/> implementation to register.
    /// </typeparam>
    /// <typeparam name="TKey">
    /// The enum that identifies the fields exposed by the schema.
    /// </typeparam>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a schema has already been registered for <typeparamref name="TKey"/>.
    /// Only one schema can be registered per field key enum.
    /// </exception>
    public static IServiceCollection AddQorelSchema<TSchema, TKey>(this IServiceCollection services)
        where TSchema : FieldSchema<TKey>
        where TKey : Enum
    {
        if (services.Any(d => d.ServiceType == typeof(FieldSchema<TKey>)))
        {
            throw new InvalidOperationException($"A FieldSchema for '{typeof(TKey).Name}' is already registered. ");
        }

        services.AddSingleton<TSchema>();
        services.AddSingleton<FieldSchema<TKey>, TSchema>();
        return services;
    }
}
