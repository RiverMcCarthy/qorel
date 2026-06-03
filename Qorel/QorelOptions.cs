namespace Qorel;

/// <summary>
/// Options for configuring the Qorel pipeline at registration time.
/// Pass a configuration delegate to <see cref="ServiceCollectionExtensions.AddQorel"/>.
/// </summary>
public sealed class QorelOptions
{
    /// <summary>
    /// Default page size when a request does not specify one. Defaults to 20.
    /// </summary>
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>
    /// Maximum allowed page size. Requests above this are clamped down silently. Defaults to 100.
    /// </summary>
    public int MaximumPageSize { get; set; } = 100;

    /// <summary>
    /// Validates the configured limits. Called by <see cref="ServiceCollectionExtensions.AddQorel"/> at startup.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either limit is not positive, or when <see cref="DefaultPageSize"/> exceeds <see cref="MaximumPageSize"/>.
    /// </exception>
    internal void Validate()
    {
        if (DefaultPageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(DefaultPageSize), DefaultPageSize, "Default page size must be greater than zero.");
        if (MaximumPageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(MaximumPageSize), MaximumPageSize, "Maximum page size must be greater than zero.");
        if (DefaultPageSize > MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(DefaultPageSize), DefaultPageSize,
                $"Default page size ({DefaultPageSize}) must not exceed the maximum page size ({MaximumPageSize}).");
        }
    }
}
