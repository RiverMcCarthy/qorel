namespace Qorel.Fields;

/// <summary>
/// A typed field definition identified by an enum key.
/// </summary>
/// <typeparam name="TKey">Enum type that identifies this field within its schema.</typeparam>
public interface IField<TKey>
{
    /// <summary>
    /// The enum key that identifies this field.
    /// </summary>
    TKey Key { get; }
}
