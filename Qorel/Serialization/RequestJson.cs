using System.Text.Json;

namespace Qorel;

/// <summary>
/// Reads requests from JSON. Properties of a request object such as <see cref="FilterRequest{TField}"/> Property names are matched
/// ignoring case, whatever the serialiser options, so requests read the same in every app.
/// </summary>
internal static class RequestJson
{
    /// <summary>
    /// Reads a JSON array of filters, e.g. from a <c>filters</c> query string parameter. <c>null</c>, blank, or a JSON
    /// <c>null</c> means no filters. The filters are not checked against a model yet.
    /// </summary>
    public static QueryResult<IReadOnlyList<FilterRequest<TField>?>> ReadFilters<TField>(string? json)
        where TField : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(json))
            return QueryResult<IReadOnlyList<FilterRequest<TField>?>>.Success([]);

        try
        {
            return QueryResult<IReadOnlyList<FilterRequest<TField>?>>.Success(
                JsonSerializer.Deserialize<List<FilterRequest<TField>?>>(json) ?? []);
        }
        catch (JsonException)
        {
            return QueryResult<IReadOnlyList<FilterRequest<TField>?>>.Failure(ValidationError.FiltersMalformed());
        }
    }

    /// <exception cref="JsonException">Thrown when the current token is not an object.</exception>
    public static JsonElement ReadObject(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a JSON object.");

        return JsonElement.ParseValue(ref reader);
    }

    /// <summary>
    /// Finds the first property called any of <paramref name="names"/>.
    /// </summary>
    public static bool TryGetProperty(JsonElement json, ReadOnlySpan<string> names, out JsonElement value)
    {
        foreach (JsonProperty property in json.EnumerateObject())
        {
            foreach (string name in names)
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Reads a required enum property, sent as a member name.
    /// </summary>
    /// <exception cref="JsonException">Thrown when the property is missing or is not a member name.</exception>
    public static TEnum ReadEnum<TEnum>(JsonElement json, string name)
        where TEnum : struct, Enum
    {
        if (!TryGetProperty(json, [name], out JsonElement property))
            throw new JsonException($"'{name}' is required.");

        return ReadEnumValue<TEnum>(property, name);
    }

    /// <summary>
    /// Reads an optional enum property, sent as a member name.
    /// </summary>
    /// <exception cref="JsonException">Thrown when the property is there but is not a member name.</exception>
    public static TEnum ReadOptionalEnum<TEnum>(JsonElement json, string name, TEnum defaultValue)
        where TEnum : struct, Enum
        => TryGetProperty(json, [name], out JsonElement property) ? ReadEnumValue<TEnum>(property, name) : defaultValue;

    private static TEnum ReadEnumValue<TEnum>(JsonElement property, string name)
        where TEnum : struct, Enum
    {
        if (property.ValueKind == JsonValueKind.String && EnumNames<TEnum>.TryParse(property.GetString(), out TEnum value))
            return value;

        throw new JsonException($"'{name}' must be the name of a {typeof(TEnum).Name}.");
    }
}
