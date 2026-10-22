using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qorel;

/// <summary>
/// Reads and writes <see cref="FilterRequest{TField}"/> as <c>{ "field": …, "operator": …, "value": … }</c>.
/// </summary>
/// <remarks>
/// A custom converter, so the field enum is read by name without the app having to register
/// <see cref="JsonStringEnumConverter"/>, and <c>"fieldKey"</c> still works for older clients.
/// </remarks>
internal sealed class FilterRequestJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(FilterRequest<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(typeToConvert.GetGenericArguments()))!;

    private sealed class Converter<TField> : JsonConverter<FilterRequest<TField>>
        where TField : struct, Enum
    {
        public override FilterRequest<TField> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonElement json = RequestJson.ReadObject(ref reader);

            return new FilterRequest<TField>
            {
                Field = RequestJson.ReadEnum<TField>(json, "field", "fieldKey"),
                Operator = RequestJson.ReadEnum<FilterOperator>(json, "operator"),
                Value = RequestJson.TryGetProperty(json, ["value"], out JsonElement value) ? value : default,
            };
        }

        public override void Write(Utf8JsonWriter writer, FilterRequest<TField> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("field", value.Field.ToString());
            writer.WriteString("operator", value.Operator.ToString());

            if (value.Value.ValueKind != JsonValueKind.Undefined)
            {
                writer.WritePropertyName("value");
                value.Value.WriteTo(writer);
            }

            writer.WriteEndObject();
        }
    }
}
