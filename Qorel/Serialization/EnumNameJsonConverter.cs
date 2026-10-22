using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qorel;

/// <summary>
/// Reads and writes enums by member name, using the strict matching in <see cref="EnumNames{TEnum}"/>.
/// </summary>
internal sealed class EnumNameJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(typeToConvert))!;

    private sealed class Converter<TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String && EnumNames<TEnum>.TryParse(reader.GetString(), out TEnum value))
                return value;

            throw new JsonException($"Expected the name of a {typeof(TEnum).Name}.");
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }
}
