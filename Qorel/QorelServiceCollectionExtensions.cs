using Qorel;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers Qorel with a service collection.
/// </summary>
public static class QorelServiceCollectionExtensions
{
    /// <summary>
    /// Registers Qorel's options. Follow it with <see cref="QorelBuilder.AddModel{TEntity,TField}"/> for each model,
    /// and a backend such as <c>AddEntityFramework</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Changes the default page sizes and request limits.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an option is out of range.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Qorel has already been added.</exception>
    public static QorelBuilder AddQorel(this IServiceCollection services, Action<QorelOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.Any(service => service.ServiceType == typeof(QorelOptions)))
            throw new InvalidOperationException("AddQorel has already been called. Call it once and chain AddModel for each model.");

        var options = new QorelOptions();
        configure?.Invoke(options);
        options.Validate();

        services.AddSingleton(options);
        return new QorelBuilder(services, options);
    }
}
