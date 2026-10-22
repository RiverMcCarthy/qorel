using System.Linq.Expressions;

namespace Qorel;

/// <summary>
/// Free-text search across a model's searchable fields. Every word of the search has to appear in at least one of them:
/// <code>
/// (Name contains "red" OR Description contains "red") AND (Name contains "shoes" OR Description contains "shoes")
/// </code>
/// </summary>
/// <remarks>
/// Each word is matched with the field's own <c>Contains</c> filter, so it follows the field's <c>IgnoreCase()</c>
/// setting and is sent as a parameter.
/// </remarks>
internal static class TextSearch
{
    /// <summary>
    /// The predicate for <paramref name="search"/>, or <c>null</c> when it has no words.
    /// </summary>
    public static Expression<Func<TEntity, bool>>? Build<TEntity, TField>(IReadOnlyList<MappedField<TEntity, TField>> fields, string? search)
        where TEntity : class
        where TField : struct, Enum
    {
        string[] words = Words(search);
        if (words.Length == 0 || fields.Count == 0)
            return null;

        ParameterExpression entity = Expression.Parameter(typeof(TEntity), "entity");

        Expression everyWord = words
            .Select(word => fields.Select(field => Contains(field, word, entity)).Aggregate(Expression.OrElse))
            .Aggregate(Expression.AndAlso);

        return Expression.Lambda<Func<TEntity, bool>>(everyWord, entity);
    }

    /// <summary>
    /// The distinct words of <paramref name="search"/>, split on whitespace.
    /// </summary>
    public static string[] Words(string? search)
        => search is null ? [] : [.. search.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal)];

    private static Expression Contains<TEntity, TField>(MappedField<TEntity, TField> field, string word, ParameterExpression entity)
        where TEntity : class
        where TField : struct, Enum
    {
        // Searchable() is only offered for text fields, so the field always takes a string filter.
        var contains = field.ToExpression(FieldFilter<TField, string>.Create(field.Field, FilterOperator.Contains, word));
        return ParameterReplacer.Replace(contains, entity);
    }
}
