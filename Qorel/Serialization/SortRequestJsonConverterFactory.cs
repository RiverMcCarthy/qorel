using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qorel.Serialization;

/// <summary>
/// Enables <see cref="System.Text.Json"/> to deserialize <see cref="SortRequest{TKey}"/>
/// with enum values expressed as strings, without requiring <see cref="JsonStringEnumConverter"/>.
/// Applied automatically via <see cref="JsonConverterAttribute"/>.
/// </summary>
public sealed class SortRequestJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(SortRequest<>);
    }

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type keyType = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(keyType))!;
    }

    private sealed class Converter<TKey> : JsonConverter<SortRequest<TKey>>
        where TKey : Enum
    {
        public override SortRequest<TKey> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object.");

            bool hasFieldKey = false;
            TKey fieldKey = default!;
            SortDirection direction = SortDirection.Asc;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected property name.");

                string propName = reader.GetString()!;
                reader.Read();

                if (propName.Equals("fieldKey", StringComparison.OrdinalIgnoreCase))
                {
                    string raw = reader.GetString()
                        ?? throw new JsonException("'fieldKey' must be a string.");

                    if (!Enum.TryParse(typeof(TKey), raw, ignoreCase: true, out object? parsed))
                        throw new JsonException($"Unknown field key: '{raw}'.");

                    fieldKey = (TKey)parsed!;
                    hasFieldKey = true;
                }
                else if (propName.Equals("direction", StringComparison.OrdinalIgnoreCase))
                {
                    string raw = reader.GetString()
                        ?? throw new JsonException("'direction' must be a string.");

                    if (!Enum.TryParse(raw, ignoreCase: true, out SortDirection parsed))
                        throw new JsonException($"Unknown sort direction: '{raw}'.");

                    direction = parsed;
                }
                else
                {
                    reader.Skip();
                }
            }

            if (!hasFieldKey)
                throw new JsonException("Required property 'fieldKey' is missing.");

            return new SortRequest<TKey> { FieldKey = fieldKey, Direction = direction };
        }

        public override void Write(Utf8JsonWriter writer, SortRequest<TKey> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("fieldKey", value.FieldKey.ToString());
            writer.WriteString("direction", value.Direction.ToString());
            writer.WriteEndObject();
        }
    }
}
