using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qorel;

/// <summary>
/// Reads and writes field values as JSON, for client filters and for cursors.
/// </summary>
/// <remarks>
/// Reading is strict: numbers must be JSON numbers, dates and Guids must be strings, and enums must be sent by name.
/// A value of the wrong shape is reported as unreadable rather than coerced.
/// </remarks>
internal static class FieldValueJson
{
    private static readonly JsonSerializerOptions _options = new()
    {
        // Lets a double cursor value of NaN or Infinity round-trip instead of failing to serialise.
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new EnumNameJsonConverter() },
    };

    /// <summary>
    /// Reads <paramref name="json"/> as a <typeparamref name="TValue"/>. Returns false when it is missing or the wrong shape,
    /// such as a string for a number or a null for an <c>int</c>.
    /// </summary>
    public static bool TryRead<TValue>(JsonElement json, out TValue value)
    {
        value = default!;

        if (json.ValueKind == JsonValueKind.Undefined)
            return false;

        try
        {
            value = json.Deserialize<TValue>(_options)!;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Writes <paramref name="value"/> in the form <see cref="TryRead{TValue}"/> reads back.
    /// </summary>
    public static JsonElement Write<TValue>(TValue value) => JsonSerializer.SerializeToElement(value, _options);

    /// <summary>
    /// Reads every item of a JSON array. Returns false when <paramref name="json"/> is not an array or any item cannot be read.
    /// </summary>
    public static bool TryReadArray<TValue>(JsonElement json, out List<TValue> values)
    {
        values = [];

        if (json.ValueKind != JsonValueKind.Array)
            return false;

        foreach (JsonElement item in json.EnumerateArray())
        {
            if (!TryRead(item, out TValue value))
                return false;

            values.Add(value);
        }

        return true;
    }
}
