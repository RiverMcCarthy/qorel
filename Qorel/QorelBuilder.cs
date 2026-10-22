using Microsoft.Extensions.DependencyInjection;

namespace Qorel;

/// <summary>
/// Registers query models and a query backend. Returned by <see cref="QorelServiceCollectionExtensions.AddQorel"/>.
/// </summary>
/// <example>
/// <code>
/// services.AddQorel()
///     .AddModel(new ProductQueryModel())
///     .AddEntityFramework&lt;AppDbContext&gt;();
/// </code>
/// </example>
public sealed class QorelBuilder
{
    internal QorelBuilder(IServiceCollection services, QorelOptions options)
    {
        Services = services;
        Options = options;
    }

    /// <summary>
    /// The service collection Qorel is registering into.
    /// </summary>
    public IServiceCollection Services { get; }

    internal QorelOptions Options { get; }

    /// <summary>
    /// Builds and checks <paramref name="model"/>, then registers it as a singleton. A mistake in the model, such as a
    /// missing id or an operator its field type cannot support, throws here, so it shows up at startup.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="serverFilters">
    /// Filters added to every query of the model, which clients cannot see or remove, e.g. to limit results to the
    /// current tenant. They are worked out once per request scope from that scope's services. Calls can still pass
    /// extra server filters of their own.
    /// </param>
    /// <example>
    /// <code>
    /// .AddModel(new ProductQueryModel(), serverFilters: services =&gt;
    ///     [ProductFields.OwnerId.EqualTo(services.GetRequiredService&lt;ICurrentUser&gt;().Id)])
    /// </code>
    /// </example>
    /// <remarks>
    /// The model is registered as <see cref="QueryModel{TEntity,TField}"/> and as its own type, so either can be injected.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the model is misconfigured, or a model for the same entity and field enum is already registered.
    /// </exception>
    public QorelBuilder AddModel<TEntity, TField>(
        QueryModel<TEntity, TField> model,
        Func<IServiceProvider, IEnumerable<Filter<TField>>>? serverFilters = null)
        where TEntity : class
        where TField : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(model);

        if (Services.Any(service => service.ServiceType == typeof(QueryModel<TEntity, TField>)))
        {
            throw new InvalidOperationException(
                $"A query model for {typeof(TEntity).Name} and {typeof(TField).Name} is already registered.");
        }

        model.Initialize(Options);

        Services.AddSingleton(model);
        Services.AddSingleton(model.GetType(), model);
        Services.AddScoped(services => new ServerFilters<TEntity, TField>([.. serverFilters?.Invoke(services) ?? []]));
        return this;
    }
}
