namespace Qorel.Spatial;

/// <summary>
/// A geographic bounding box defined by latitude/longitude ranges.
/// Passed via <c>QueryRequest.SpatialBounds</c> or <c>QueryBuilder.WithSpatialBounds()</c>
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
        if (minLatitude is < (-90) or > 90)
            throw new ArgumentOutOfRangeException(nameof(minLatitude), minLatitude, "Latitude must be in [-90, 90].");
        if (maxLatitude is < (-90) or > 90)
            throw new ArgumentOutOfRangeException(nameof(maxLatitude), maxLatitude, "Latitude must be in [-90, 90].");
        if (minLatitude > maxLatitude)
            throw new ArgumentOutOfRangeException(nameof(minLatitude), "minLatitude must be ≤ maxLatitude.");

        if (minLongitude is < (-180) or > 180)
            throw new ArgumentOutOfRangeException(nameof(minLongitude), minLongitude, "Longitude must be in [-180, 180].");
        if (maxLongitude is < (-180) or > 180)
            throw new ArgumentOutOfRangeException(nameof(maxLongitude), maxLongitude, "Longitude must be in [-180, 180].");
        if (minLongitude > maxLongitude)
            throw new ArgumentOutOfRangeException(nameof(minLongitude), "minLongitude must be ≤ maxLongitude.");

        return new BoundingBox(minLatitude, minLongitude, maxLatitude, maxLongitude);
    }
}
