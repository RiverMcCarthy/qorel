namespace Qorel;

/// <summary>
/// Field options that only apply to some value types, written as extensions so the compiler enforces that.
/// </summary>
public static class FieldBuilderExtensions
{
    /// <summary>
    /// Lets clients sort by the field. Only ordered types such as numbers, dates, and strings can be sorted.
    /// </summary>
    /// <remarks>
    /// Index the column, since sorted pages read it in order. A nullable field sorts its nulls last ascending and first
    /// descending on every database.
    /// </remarks>
    public static FieldBuilder<TEntity, TField, TValue> Sortable<TEntity, TField, TValue>(this FieldBuilder<TEntity, TField, TValue> field)
        where TEntity : class
        where TField : struct, Enum
        where TValue : IComparable<TValue>
    {
        ArgumentNullException.ThrowIfNull(field);

        field.SetSortable();
        return field;
    }

    /// <inheritdoc cref="Sortable{TEntity, TField, TValue}(FieldBuilder{TEntity, TField, TValue})"/>
    public static FieldBuilder<TEntity, TField, TValue?> Sortable<TEntity, TField, TValue>(this FieldBuilder<TEntity, TField, TValue?> field)
        where TEntity : class
        where TField : struct, Enum
        where TValue : struct, IComparable<TValue>
    {
        ArgumentNullException.ThrowIfNull(field);

        field.SetSortable();
        return field;
    }

    /// <summary>
    /// Matches a request's <c>search</c> text against the field. Every word of the search has to appear in at least one
    /// searchable field, so "red shoes" finds "Red running shoes".
    /// </summary>
    /// <remarks>
    /// Search uses <c>Contains</c>, which cannot use a normal index, so keep it to the few fields people actually search.
    /// Add <c>IgnoreCase()</c> as well so the search ignores case on every database.
    /// </remarks>
    public static FieldBuilder<TEntity, TField, string> Searchable<TEntity, TField>(this FieldBuilder<TEntity, TField, string> field)
        where TEntity : class
        where TField : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(field);

        field.SetSearchable();
        return field;
    }

    /// <summary>
    /// Ignores case when filtering the field, both in the database and in memory. Sorting is unaffected.
    /// </summary>
    /// <remarks>
    /// The database compares <c>LOWER(column)</c>, so a plain index on the column cannot help <c>Equals</c> or <c>IsOneOf</c>.
    /// On a big table, index the lowercased column instead.
    /// </remarks>
    public static FieldBuilder<TEntity, TField, string> IgnoreCase<TEntity, TField>(this FieldBuilder<TEntity, TField, string> field)
        where TEntity : class
        where TField : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(field);

        field.SetIgnoreCase();
        return field;
    }
}
