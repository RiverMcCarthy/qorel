using System.Collections.Frozen;
using System.Text.Json;

using Qorel.Fields;

namespace Qorel.Filters;

internal interface IFilterValueParser
{
    FrozenSet<FilterOperator> ValidOperators { get; }
    IFilter<TKey> Build<TKey>(IField<TKey> field, FilterOperator op, JsonElement value) where TKey : Enum;
}

internal sealed class FilterValueParser<T>(FrozenSet<FilterOperator> validOperators, Func<JsonElement, T> parse)
    : IFilterValueParser
{
    public FrozenSet<FilterOperator> ValidOperators { get; } = validOperators;

    public IFilter<TKey> Build<TKey>(IField<TKey> field, FilterOperator op, JsonElement value)
        where TKey : Enum
    {
        if (field is not Field<TKey, T> typedField)
        {
            throw new InvalidOperationException(
                $"Field '{field.Key}' is declared as '{field.GetType().Name}' but the parser expected '{typeof(T).Name}'.");
        }

        return op switch
        {
            FilterOperator.IsOneOf or FilterOperator.IsNotOneOf =>
                new CollectionFilter<TKey, T>(op)
                {
                    Field = typedField,
                    Values = [.. value.EnumerateArray().Select(parse)],
                },

            // IsNull/IsNotNull carry no value — the operator alone is the predicate.
            FilterOperator.IsNull or FilterOperator.IsNotNull =>
                new NullCheckFilter<TKey, T>(op) { Field = typedField },

            _ =>
                new ScalarFilter<TKey, T>(op)
                {
                    Field = typedField,
                    Value = parse(value),
                },
        };
    }
}
