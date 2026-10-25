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
    /// <remarks>
    /// The model is registered as <see cref="QueryModel{TEntity,TField}"/> and as its own type, so either can be injected.
    /// If every query for the model needs a filter, like scoping to the current user, register an
    /// <see cref="IServerFilter{TEntity}"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the model is misconfigured, or a model for the same entity and field enum is already registered.
    /// </exception>
    public QorelBuilder AddModel<TEntity, TField>(QueryModel<TEntity, TField> model)
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
        return this;
    }
}
