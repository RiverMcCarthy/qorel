namespace Qorel;

/// <summary>
/// A geographic bounding box defined by latitude/longitude ranges.
/// Passed as <c>QueryRequest.SpatialBounds</c> or with <c>QueryBuilder.WithSpatialBounds()</c>
/// to filter results to items where the spatial field intersects the box.
/// </summary>
/// <remarks>
/// Prefer <see cref="Create"/> when constructing from user input as it validates coordinates.
/// The primary constructor is used for JSON deserialisation.
/// </remarks>
public readonly record struct BoundingBox(
    double MinLatitude,
    double MinLongitude,
    double MaxLatitude,
    double MaxLongitude)
{
    /// <summary>
    /// Creates a <see cref="BoundingBox"/> with coordinate validation.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any coordinate is out of the valid geographic range, or when a minimum exceeds its corresponding maximum.
    /// </exception>
    public static BoundingBox Create(
        double minLatitude,
        double minLongitude,
        double maxLatitude,
        double maxLongitude)
    {
        if (!IsLatitude(minLatitude))
            throw new ArgumentOutOfRangeException(nameof(minLatitude), minLatitude, "Latitude must be in [-90, 90].");
        if (!IsLatitude(maxLatitude))
            throw new ArgumentOutOfRangeException(nameof(maxLatitude), maxLatitude, "Latitude must be in [-90, 90].");
        if (minLatitude > maxLatitude)
            throw new ArgumentOutOfRangeException(nameof(minLatitude), "minLatitude must be ≤ maxLatitude.");

        if (!IsLongitude(minLongitude))
            throw new ArgumentOutOfRangeException(nameof(minLongitude), minLongitude, "Longitude must be in [-180, 180].");
        if (!IsLongitude(maxLongitude))
            throw new ArgumentOutOfRangeException(nameof(maxLongitude), maxLongitude, "Longitude must be in [-180, 180].");
        if (minLongitude > maxLongitude)
            throw new ArgumentOutOfRangeException(nameof(minLongitude), "minLongitude must be ≤ maxLongitude.");

        return new BoundingBox(minLatitude, minLongitude, maxLatitude, maxLongitude);
    }

    /// <summary>
    /// True if every coordinate is in range and each minimum is less than or equal to its maximum.
    /// Used to validate boxes that came in through JSON and skipped <see cref="Create"/>.
    /// </summary>
    internal bool IsValid =>
        IsLatitude(MinLatitude) && IsLatitude(MaxLatitude) && MinLatitude <= MaxLatitude &&
        IsLongitude(MinLongitude) && IsLongitude(MaxLongitude) && MinLongitude <= MaxLongitude;

    // Written as positive ranges so NaN fails the check too.
    private static bool IsLatitude(double value) => value is >= -90 and <= 90;

    private static bool IsLongitude(double value) => value is >= -180 and <= 180;
}
