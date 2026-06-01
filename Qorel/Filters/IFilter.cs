namespace Qorel.Filters;

/// <summary>
/// Represents a typed filter condition for a specific field.
/// </summary>
/// <typeparam name="TKey">Enum type that identifies fields in the schema.</typeparam>
public interface IFilter<TKey> where TKey : Enum
{
    /// <summary>
    /// The enum key of the field this filter targets.
    /// </summary>
    TKey FieldKey { get; }

    /// <summary>
    /// The comparison operator to apply.
    /// </summary>
    FilterOperator Operator { get; }
}
