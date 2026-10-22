namespace Qorel;

/// <summary>
/// One column of a page's sort order: the field, and which way it sorts.
/// </summary>
internal sealed record SortKey<TEntity, TField>(MappedField<TEntity, TField> Field, bool Descending)
    where TEntity : class
    where TField : struct, Enum;
