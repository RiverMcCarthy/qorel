namespace Qorel.EntityFramework.Cursors;

/// <summary>
/// Serialised form of a cursor.
/// Each element in <see cref="Values"/> is the JSON-serialised value of the
/// corresponding sort field from the last row on the previous page.
/// </summary>
internal sealed record CursorToken(string[] Values);
