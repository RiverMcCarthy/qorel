using System.Collections.Frozen;
using System.Text.Json;

using Qorel.Schema;

namespace Qorel.Filters;

/// <summary>
/// Provides the built-in parsers and the factory method for creating enum parsers.
/// Enum parsers live on the <see cref="FieldSchema{TKey}"/> instance that declared them, avoiding shared mutable static state.
/// </summary>
internal static class FilterFactory
{
    internal static readonly FrozenDictionary<Type, IFilterValueParser> BuiltInParsers =
        new Dictionary<Type, IFilterValueParser>
        {
            [typeof(bool)] = Parser(FilterOperators.ForBool, e => e.GetBoolean()),
            [typeof(int)] = Parser(FilterOperators.ForNumber, e => e.GetInt32()),
            [typeof(long)] = Parser(FilterOperators.ForNumber, e => e.GetInt64()),
            [typeof(float)] = Parser(FilterOperators.ForNumber, e => (float)e.GetDouble()),
            [typeof(double)] = Parser(FilterOperators.ForNumber, e => e.GetDouble()),
            [typeof(decimal)] = Parser(FilterOperators.ForNumber, e => e.GetDecimal()),
            [typeof(Guid)] = Parser(FilterOperators.ForGuid, e => e.GetGuid()),
            [typeof(DateTime)] = Parser(FilterOperators.ForDate, e => e.GetDateTime()),
            [typeof(DateTimeOffset)] = Parser(FilterOperators.ForDate, e => e.GetDateTimeOffset()),
            [typeof(TimeSpan)] = Parser(FilterOperators.Comparison, e => TimeSpan.Parse(e.GetString()!)),
            [typeof(string)] = new FilterValueParser<string>(FilterOperators.ForString, e => e.ValueKind == JsonValueKind.Null ? null! : e.GetString()!),
        }.ToFrozenDictionary();

    /// <summary>
    /// Creates a parser for an enum type using the compile-time generic parameter.
    /// Called from <see cref="FieldSchema{TKey}.For{TProp}"/> where the type parameter is known at compile time.
    /// </summary>
    internal static FilterValueParser<TValue> CreateEnumParser<TValue>()
        => new(FilterOperators.ForEnum,
            e => (TValue)Enum.Parse(typeof(TValue), e.GetString()!, ignoreCase: true));

    private static FilterValueParser<T> Parser<T>(FrozenSet<FilterOperator> ops, Func<JsonElement, T> parse)
        => new(ops, parse);
}
