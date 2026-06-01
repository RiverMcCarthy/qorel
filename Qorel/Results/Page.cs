namespace Qorel.Results;

/// <summary>
/// A single page of results from a cursor-paginated query.
/// Pass <see cref="NextCursor"/> as the <c>cursor</c> on the next request to fetch the subsequent page.
/// </summary>
/// <typeparam name="T">The DTO type of each result item.</typeparam>
public sealed record Page<T>
{
    /// <summary>
    /// The items on this page.
    /// </summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// A cursor for fetching the next page. <c>null</c> when there are no further pages.
    /// </summary>
    public string? NextCursor { get; init; }

    /// <summary>
    /// The number of items on this page.
    /// </summary>
    public int Count => Items.Count;

    /// <summary>
    /// True if there are more items.
    /// </summary>
    public bool HasNextPage => NextCursor is not null;

    /// <summary>
    /// True if this page contains no items.
    /// </summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>
    /// Returns an empty page with no items and no next cursor.
    /// </summary>
    public static Page<T> Empty() => new() { Items = Array.Empty<T>() };
}
