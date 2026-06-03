using System.Text.Json;

using Qorel.Exceptions;

namespace Qorel.EntityFramework.Cursors;

/// <summary>
/// Encodes and decodes <see cref="CursorToken"/> values as base64 strings
/// for safe transmission in API responses.
/// </summary>
internal static class CursorEncoder
{
    private const string _invalidCursorMessage = "The pagination cursor is invalid or has been tampered with.";

    /// <summary>
    /// Encodes a cursor token into a JSON serialised string
    /// </summary>
    public static string Encode(CursorToken token)
        => Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(token));

    /// <summary>
    /// Decodes a cursor string into a <see cref="CursorToken"/>
    /// </summary>
    /// <exception cref="InvalidCursorException">Thrown when the cursor is malformed or tampered with.</exception>
    public static CursorToken Decode(string cursor)
    {
        try
        {
            var bytes = Convert.FromBase64String(cursor);

            return JsonSerializer.Deserialize<CursorToken>(bytes)
                ?? throw new InvalidCursorException(_invalidCursorMessage);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            throw new InvalidCursorException(_invalidCursorMessage, ex);
        }
    }
}
