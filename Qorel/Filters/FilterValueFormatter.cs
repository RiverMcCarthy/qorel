using System.Globalization;
using System.Text.Json;

namespace Qorel;

/// <summary>
/// Writes filter values the same way every time, whatever the culture, for <see cref="Filter{TField}.CacheKey"/>.
/// </summary>
internal static class FilterValueFormatter
{
    /// <summary>
    /// Formats <paramref name="value"/> as a JSON-style token. Values that mean the same thing format the same,
    /// e.g. <c>5000m</c> and <c>5000.0m</c>, or two <see cref="DateTimeOffset"/> values at the same instant.
    /// </summary>
    public static string Format(object? value) => value switch
    {
        null => "null",

        // Quoted and escaped, so a value can never be mistaken for the separators around it.
        string text => JsonSerializer.Serialize(text),
        Enum member => JsonSerializer.Serialize(member.ToString()),

        bool flag => flag ? "true" : "false",
        decimal number => FormatDecimal(number),
        double number => FormatDouble(number),
        float number => FormatSingle(number),
        DateTimeOffset date => date.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTime date => (date.Kind == DateTimeKind.Local ? date.ToUniversalTime() : date).ToString("O", CultureInfo.InvariantCulture),
        DateOnly date => date.ToString("O", CultureInfo.InvariantCulture),
        TimeOnly time => time.ToString("O", CultureInfo.InvariantCulture),
        Guid id => id.ToString("D"),
        TimeSpan span => span.ToString("c", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => JsonSerializer.Serialize(value.ToString()),
    };

    // Dividing by a one with many trailing zeros drops the scale, so 5000.0m becomes 5000m.
    private static string FormatDecimal(decimal number)
        => number == 0 ? "0" : (number / 1.000000000000000000000000000000000m).ToString(CultureInfo.InvariantCulture);

    // "R" is the shortest string that round-trips, so 5000.0 is "5000". Zero is written out so -0 matches 0.
    private static string FormatDouble(double number)
        => number == 0 ? "0" : number.ToString("R", CultureInfo.InvariantCulture);

    // Formatted as a float rather than widened to double, so 0.1f is "0.1" and not "0.10000000149011612".
    private static string FormatSingle(float number)
        => number == 0 ? "0" : number.ToString("R", CultureInfo.InvariantCulture);
}
