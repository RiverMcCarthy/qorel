using System.Collections.Frozen;

namespace Qorel;

/// <summary>
/// Looks up enum members by name, ignoring case. Unlike <see cref="Enum.TryParse{TEnum}(string?, bool, out TEnum)"/>,
/// it never accepts a number (<c>"3"</c>) or a combination (<c>"A, B"</c>), so a client can only send a real member.
/// </summary>
internal static class EnumNames<TEnum>
    where TEnum : struct, Enum
{
    private static readonly FrozenDictionary<string, TEnum> _byName = CreateLookup();

    public static bool TryParse(string? name, out TEnum value)
    {
        if (name is not null)
            return _byName.TryGetValue(name, out value);

        value = default;
        return false;
    }

    private static FrozenDictionary<string, TEnum> CreateLookup()
    {
        var byName = new Dictionary<string, TEnum>(StringComparer.OrdinalIgnoreCase);

        // TryAdd, because two members can differ only by case. The first one declared wins.
        foreach (string name in Enum.GetNames<TEnum>())
            byName.TryAdd(name, Enum.Parse<TEnum>(name));

        return byName.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}
