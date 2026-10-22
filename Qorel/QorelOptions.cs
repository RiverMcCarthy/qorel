namespace Qorel;

/// <summary>
/// Page sizes and request limits that apply to every query. Set them with
/// <see cref="Microsoft.Extensions.DependencyInjection.QorelServiceCollectionExtensions.AddQorel"/>.
/// </summary>
/// <remarks>
/// The limits keep a single client request from building a huge SQL statement. A request over a limit gets a
/// validation error rather than being trimmed, so the client knows its query was not run as sent.
/// </remarks>
public sealed class QorelOptions
{
    /// <summary>
    /// The page size used when a request does not ask for one. Defaults to 20.
    /// </summary>
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>
    /// The largest page a client can ask for. Larger requests are lowered to this without an error. Defaults to 100.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// The most filters a request can have. Defaults to 50.
    /// </summary>
    public int MaxFilters { get; set; } = 50;

    /// <summary>
    /// The most values an <c>IsOneOf</c> or <c>IsNotOneOf</c> filter can have. Defaults to 500.
    /// </summary>
    public int MaxFilterValues { get; set; } = 500;

    /// <summary>
    /// The most sorts a request can have. Defaults to 5.
    /// </summary>
    public int MaxSorts { get; set; } = 5;

    /// <summary>
    /// The longest search text accepted, in characters. Defaults to 100.
    /// </summary>
    public int MaxSearchLength { get; set; } = 100;

    /// <summary>
    /// The longest cursor accepted, in characters. Defaults to 4096.
    /// </summary>
    public int MaxCursorLength { get; set; } = 4096;

    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a setting is not positive, or <see cref="DefaultPageSize"/> is bigger than <see cref="MaxPageSize"/>.
    /// </exception>
    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(DefaultPageSize, 1, nameof(DefaultPageSize));
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxPageSize, 1, nameof(MaxPageSize));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(DefaultPageSize, MaxPageSize, nameof(DefaultPageSize));
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxFilters, 1, nameof(MaxFilters));
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxFilterValues, 1, nameof(MaxFilterValues));
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxSorts, 1, nameof(MaxSorts));
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxSearchLength, 1, nameof(MaxSearchLength));
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxCursorLength, 1, nameof(MaxCursorLength));
    }
}
