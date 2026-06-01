using Qorel.Schema;

namespace Qorel.Fields;

/// <summary>
/// A typed field definition for enum values, extending <see cref="Field{TKey,TValue}"/>
/// The enum parser is registered automatically when the field is added to a schema.
/// </summary>
/// <typeparam name="TKey">The schema field key type.</typeparam>
/// <typeparam name="TEnum">The enum value type.</typeparam>
public sealed record EnumField<TKey, TEnum>(TKey Key) : Field<TKey, TEnum>(Key)
    where TKey : Enum
    where TEnum : struct, Enum;
