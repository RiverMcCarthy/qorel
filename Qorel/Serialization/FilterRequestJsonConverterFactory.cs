using System.Text.Json;
using System.Text.Json.Serialization;

using Qorel.Filters;

namespace Qorel.Serialization;

/// <summary>
/// Enables <see cref="System.Text.Json"/> to deserialize <see cref="FilterRequest{TKey}"/>
/// with enum values expressed as strings, without requiring <see cref="JsonStringEnumConverter"/>.
/// Applied automatically via <see cref="JsonConverterAttribute"/>.
/// </summary>
public sealed class FilterRequestJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(FilterRequest<>);
    }

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type keyType = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(keyType))!;
    }

    private sealed class Converter<TKey> : JsonConverter<FilterRequest<TKey>>
        where TKey : Enum
    {
        public override FilterRequest<TKey> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object.");

            bool hasFieldKey = false;
            TKey fieldKey = default!;
            bool hasOperator = false;
            FilterOperator filterOperator = default;
            bool hasValue = false;
            JsonElement value = default;

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
                else if (propName.Equals("operator", StringComparison.OrdinalIgnoreCase))
                {
                    string raw = reader.GetString()
                        ?? throw new JsonException("'operator' must be a string.");

                    if (!Enum.TryParse(raw, ignoreCase: true, out FilterOperator parsed))
                        throw new JsonException($"Unknown operator: '{raw}'.");

                    filterOperator = parsed;
                    hasOperator = true;
                }
                else if (propName.Equals("value", StringComparison.OrdinalIgnoreCase))
                {
                    value = JsonElement.ParseValue(ref reader);
                    hasValue = true;
                }
                else
                {
                    reader.Skip();
                }
            }

            if (!hasFieldKey)
                throw new JsonException("Required property 'fieldKey' is missing.");
            if (!hasOperator)
                throw new JsonException("Required property 'operator' is missing.");
            if (!hasValue)
                throw new JsonException("Required property 'value' is missing.");

            return new FilterRequest<TKey>
            {
                FieldKey = fieldKey,
                Operator = filterOperator,
                Value = value,
            };
        }

        public override void Write(Utf8JsonWriter writer, FilterRequest<TKey> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("fieldKey", value.FieldKey.ToString());
            writer.WriteString("operator", value.Operator.ToString());
            writer.WritePropertyName("value");
            value.Value.WriteTo(writer);
            writer.WriteEndObject();
        }
    }
}
