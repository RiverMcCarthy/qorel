using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qorel;

/// <summary>
/// Writes and reads cursors. A cursor records where a page ended: the last item's value in each sort key.
/// </summary>
/// <remarks>
/// <para>
/// A cursor is URL-safe base64 of <c>{"sort":"Price:Desc,Id:Asc","values":[19.99,"…"]}</c>. The sort is included so a
/// cursor cannot be used with different sorts, where its values would mean something else.
/// </para>
/// <para>
/// Cursors are not signed. Editing one only moves where the next page starts, and the query's filters still apply,
/// so it cannot reveal anything the client could not already page to.
/// </para>
/// </remarks>
internal static class Cursor
{
    public static string Encode<TEntity, TField>(IReadOnlyList<SortKey<TEntity, TField>> keys, IReadOnlyList<object?> values)
        where TEntity : class
        where TField : struct, Enum
    {
        var payload = new Payload(
            Signature(keys),
            [.. keys.Select((key, i) => key.Field.WriteCursorValue(values[i]))]);

        return ToBase64Url(JsonSerializer.SerializeToUtf8Bytes(payload));
    }

    /// <summary>
    /// Reads the sort values out of <paramref name="cursor"/>. Returns false when it is malformed, too long, or was
    /// created for different sorts.
    /// </summary>
    public static bool TryDecode<TEntity, TField>(
        string cursor,
        IReadOnlyList<SortKey<TEntity, TField>> keys,
        int maxLength,
        out object?[] values)
        where TEntity : class
        where TField : struct, Enum
    {
        values = [];

        if (cursor.Length > maxLength || !TryFromBase64Url(cursor, out byte[] bytes))
            return false;

        Payload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<Payload>(bytes);
        }
        catch (JsonException)
        {
            return false;
        }

        if (payload?.Values is null || payload.Sort != Signature(keys) || payload.Values.Length != keys.Count)
            return false;

        object?[] decoded = new object?[keys.Count];
        for (int i = 0; i < keys.Count; i++)
        {
            if (!keys[i].Field.TryReadCursorValue(payload.Values[i], out decoded[i]))
                return false;
        }

        values = decoded;
        return true;
    }

    private static string Signature<TEntity, TField>(IReadOnlyList<SortKey<TEntity, TField>> keys)
        where TEntity : class
        where TField : struct, Enum
        => string.Join(',', keys.Select(key => $"{key.Field.Field}:{key.Direction}"));

    private static string ToBase64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool TryFromBase64Url(string text, out byte[] bytes)
    {
        StringBuilder base64 = new StringBuilder(text.Length + 3).Append(text).Replace('-', '+').Replace('_', '/');
        base64.Append('=', (4 - (base64.Length % 4)) % 4);

        bytes = new byte[base64.Length];
        if (!Convert.TryFromBase64String(base64.ToString(), bytes, out int written))
            return false;

        bytes = bytes[..written];
        return true;
    }

    private sealed record Payload(
        [property: JsonPropertyName("sort")] string Sort,
        [property: JsonPropertyName("values")] JsonElement[] Values);
}
