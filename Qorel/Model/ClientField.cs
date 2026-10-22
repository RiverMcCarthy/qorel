namespace Qorel;

/// <summary>
/// What clients may do with one field. <see cref="QueryModel{TEntity,TField}.ClientFields"/> lists them, which is useful
/// for an endpoint that tells clients what they can filter, sort, and search.
/// </summary>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
public sealed class ClientField<TField>
    where TField : struct, Enum
{
    internal ClientField(TField field, Type valueType, IReadOnlySet<FilterOperator> allowedOperators, bool isSortable, bool isSearchable)
    {
        Field = field;
        ValueType = valueType;
        AllowedOperators = allowedOperators;
        IsSortable = isSortable;
        IsSearchable = isSearchable;
    }

    /// <summary>
    /// The field.
    /// </summary>
    public TField Field { get; }

    /// <summary>
    /// The field's value type.
    /// </summary>
    public Type ValueType { get; }

    /// <summary>
    /// True if clients can filter on the field.
    /// </summary>
    public bool IsFilterable => AllowedOperators.Count > 0;

    /// <summary>
    /// The operators clients can filter the field with. Empty when it is not filterable.
    /// </summary>
    public IReadOnlySet<FilterOperator> AllowedOperators { get; }

    /// <summary>
    /// True if clients can sort by the field.
    /// </summary>
    public bool IsSortable { get; }

    /// <summary>
    /// True if a request's <c>search</c> text is matched against the field.
    /// </summary>
    public bool IsSearchable { get; }

    /// <summary>
    /// True if clients can use the field at all. Other fields are for server-side filters only, and requests that name
    /// them are rejected as if the field did not exist.
    /// </summary>
    internal bool IsVisible => IsFilterable || IsSortable || IsSearchable;
}
