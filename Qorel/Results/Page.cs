namespace Qorel;

/// <summary>
/// One page of results. Send <see cref="NextCursor"/> back as the cursor to get the next page.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed record Page<T>
{
    /// <summary>
    /// A page with no items and no next page.
    /// </summary>
    public static Page<T> Empty { get; } = new() { Items = [] };

    /// <summary>
    /// The items on this page.
    /// </summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// The cursor for the next page, or <c>null</c> when this is the last page.
    /// </summary>
    public string? NextCursor { get; init; }

    /// <summary>
    /// The number of items on this page.
    /// </summary>
    public int Count => Items.Count;

    /// <summary>
    /// True when there is another page.
    /// </summary>
    public bool HasNextPage => NextCursor is not null;

    /// <summary>
    /// True when this page has no items.
    /// </summary>
    public bool IsEmpty => Items.Count == 0;
}
