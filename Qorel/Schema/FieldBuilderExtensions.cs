namespace Qorel.Schema;

/// <summary>
/// Extension methods on <see cref="FieldBuilder{TKey,TProp}"/>.
/// </summary>
public static class FieldBuilderExtensions
{
    /// <summary>
    /// Marks a field as sortable.
    /// The <see cref="IComparable{T}"/> constraint ensures only ordered types can be sorted.
    /// Defined as extensions so the <see cref="IComparable{T}"/> constraint on
    /// <see cref="Sortable{TKey,TProp}"/> is only enforced on comparable field types.
    /// </summary>
    public static FieldBuilder<TKey, TProp> Sortable<TKey, TProp>(
        this FieldBuilder<TKey, TProp> builder)
        where TKey : Enum
        where TProp : IComparable<TProp>
    {
        builder.MarkSortable();
        return builder;
    }
}
