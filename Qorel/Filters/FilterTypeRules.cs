using System.Collections.Frozen;

using Qorel.Schema;

namespace Qorel.Filters;

/// <summary>
/// Maps CLR types to their default allowed <see cref="FilterOperator"/> sets.
/// Used by <see cref="FieldBuilder{TKey,TProp}"/> when inferring operators from a field's CLR type.
/// </summary>
internal static class FilterTypeRules
{
    private static readonly FrozenDictionary<Type, IReadOnlySet<FilterOperator>> _map =
        new Dictionary<Type, IReadOnlySet<FilterOperator>>
        {
            [typeof(bool)] = FilterOperators.ForBool,
            [typeof(Guid)] = FilterOperators.ForGuid,
            [typeof(string)] = FilterOperators.ForString,
            [typeof(int)] = FilterOperators.ForNumber,
            [typeof(long)] = FilterOperators.ForNumber,
            [typeof(float)] = FilterOperators.ForNumber,
            [typeof(double)] = FilterOperators.ForNumber,
            [typeof(decimal)] = FilterOperators.ForNumber,
            [typeof(DateTime)] = FilterOperators.ForDate,
            [typeof(DateTimeOffset)] = FilterOperators.ForDate,
            [typeof(TimeSpan)] = FilterOperators.Comparison,
        }.ToFrozenDictionary();

    /// <summary>
    /// Returns the default operator set for <paramref name="type"/>, unwrapping
    /// <see cref="Nullable{T}"/> automatically and adding null-check operators for nullable types.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="type"/> has no registered operator set.
    /// </exception>
    public static IReadOnlySet<FilterOperator> GetValidOperators(Type type)
    {
        bool isNullableValueType = Nullable.GetUnderlyingType(type) is not null;
        Type t = Nullable.GetUnderlyingType(type) ?? type;

        if (!_map.TryGetValue(t, out IReadOnlySet<FilterOperator>? baseOps))
        {
            if (!t.IsEnum)
            {
                throw new InvalidOperationException(
                    $"No filter operators defined for type '{t.Name}'. " +
                    $"Supported: bool, numeric types, Guid, DateTime, DateTimeOffset, TimeSpan, string, and enum types.");
            }

            baseOps = FilterOperators.ForEnum;
        }

        // Nullable<T> value types automatically gain null-check operators.
        // String is a reference type handled directly in the map via ForString which includes NullCheck.
        return isNullableValueType
            ? baseOps.Concat(FilterOperators.NullCheck).ToFrozenSet()
            : baseOps;
    }
}
