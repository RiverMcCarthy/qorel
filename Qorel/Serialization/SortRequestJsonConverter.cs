using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qorel;

/// <summary>
/// Reads and writes <see cref="SortRequest{TField}"/> as <c>{ "field": …, "direction": … }</c>.
/// See <see cref="FilterRequestJsonConverter"/> for why it is custom.
/// </summary>
internal sealed class SortRequestJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(SortRequest<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(typeToConvert.GetGenericArguments()))!;

    private sealed class Converter<TField> : JsonConverter<SortRequest<TField>>
        where TField : struct, Enum
    {
        public override SortRequest<TField> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonElement json = RequestJson.ReadObject(ref reader);

            return new SortRequest<TField>
            {
                Field = RequestJson.ReadEnum<TField>(json, "field"),
                Direction = RequestJson.ReadOptionalEnum(json, "direction", SortDirection.Asc),
            };
        }

        public override void Write(Utf8JsonWriter writer, SortRequest<TField> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("field", value.Field.ToString());
            writer.WriteString("direction", value.Direction.ToString());
            writer.WriteEndObject();
        }
    }
}
