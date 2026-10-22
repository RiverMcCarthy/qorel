namespace Qorel;

/// <summary>
/// A handle on a <see cref="string"/> field. Adds substring filters to the ones on <see cref="Field{TField,TValue}"/>.
/// </summary>
/// <remarks>
/// Text matching follows the database's collation unless the field is mapped with <c>IgnoreCase()</c>.
/// </remarks>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
/// <param name="key">The enum member that names this field.</param>
public sealed class TextField<TField>(TField key) : Field<TField, string>(key)
    where TField : struct, Enum
{
    /// <summary>
    /// Matches items whose value contains <paramref name="value"/>.
    /// </summary>
    public Filter<TField> Contains(string value)
        => FieldFilter<TField, string>.Create(Key, FilterOperator.Contains, value);

    /// <summary>
    /// Matches items whose value starts with <paramref name="value"/>.
    /// </summary>
    public Filter<TField> StartsWith(string value)
        => FieldFilter<TField, string>.Create(Key, FilterOperator.StartsWith, value);

    /// <summary>
    /// Matches items whose value ends with <paramref name="value"/>.
    /// </summary>
    public Filter<TField> EndsWith(string value)
        => FieldFilter<TField, string>.Create(Key, FilterOperator.EndsWith, value);
}
