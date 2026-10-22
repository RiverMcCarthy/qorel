namespace Qorel;

/// <summary>
/// A sort that has been checked against the model.
/// </summary>
internal readonly record struct Sort<TField>(TField Field, SortDirection Direction)
    where TField : struct, Enum
{
    public bool Descending => Direction == SortDirection.Desc;
}
