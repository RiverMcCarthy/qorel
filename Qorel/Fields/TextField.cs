using Qorel.Filters;

namespace Qorel.Fields;

/// <summary>
/// A typed field definition for <see cref="string"/> values,
/// extending <see cref="Field{TKey,TValue}"/> with substring filter operators.
/// </summary>
/// <typeparam name="TKey">Enum type identifying fields in the schema.</typeparam>
public sealed record TextField<TKey>(TKey Key) : Field<TKey, string>(Key)
    where TKey : Enum
{
    /// <summary>
    /// Creates a <c>Contains</c> filter. Case sensitivity depends on the database.
    /// </summary>
    public IFilter<TKey> Contains(string value)
        => new ScalarFilter<TKey, string>(FilterOperator.Contains) { Field = this, Value = value };

    /// <summary>
    /// Creates a <c>StartsWith</c> prefix filter. Case sensitivity depends on the database.
    /// </summary>
    public IFilter<TKey> StartsWith(string value)
        => new ScalarFilter<TKey, string>(FilterOperator.StartsWith) { Field = this, Value = value };

    /// <summary>
    /// Creates an <c>EndsWith</c> suffix filter. Case sensitivity depends on the database.
    /// </summary>
    public IFilter<TKey> EndsWith(string value)
        => new ScalarFilter<TKey, string>(FilterOperator.EndsWith) { Field = this, Value = value };
}
