namespace Qorel;

/// <summary>
/// One column of a page's sort order: the field, and which way it sorts.
/// </summary>
internal sealed record SortKey<TEntity, TField>(MappedField<TEntity, TField> Field, SortDirection Direction)
    where TEntity : class
    where TField : struct, Enum
{
    public bool Descending => Direction == SortDirection.Desc;
}
