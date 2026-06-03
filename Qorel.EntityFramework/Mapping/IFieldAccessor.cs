using System.Linq.Expressions;

using Qorel.Filters;

namespace Qorel.EntityFramework.Mapping;

/// <summary>
/// Strongly-typed accessor over a single entity field, enabling dynamic query composition
/// for filtering, sorting, and cursor serialisation. No reflection at query time.
/// </summary>
/// <typeparam name="TEntity">The EF Core entity type.</typeparam>
internal interface IFieldAccessor<TEntity>
{
    /// <summary>
    /// The CLR type of the field value.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Applies a primary ordering by this field.
    /// </summary>
    IOrderedQueryable<TEntity> OrderBy(IQueryable<TEntity> source, bool descending);

    /// <summary>
    /// Applies a secondary ordering by this field.
    /// </summary>
    IOrderedQueryable<TEntity> ThenBy(IOrderedQueryable<TEntity> source, bool descending);

    /// <summary>
    /// Returns the field body expression with its parameter replaced by <paramref name="param"/>.
    /// Used when composing multi-field expressions that share a single parameter.
    /// </summary>
    Expression Inline(ParameterExpression param);

    /// <summary>
    /// Builds a predicate that asserts this field equals <paramref name="value"/>.
    /// Used by <c>QueryPipeline.FindAsync</c> to avoid requiring a marker interface on the entity.
    /// </summary>
    Expression<Func<TEntity, bool>> BuildEqualityPredicate(object value);

    /// <summary>
    /// Builds a predicate expression for the given filter, translated to SQL by EF Core.
    /// </summary>
    Expression<Func<TEntity, bool>> BuildPredicate<TKey>(IFilter<TKey> filter) where TKey : Enum;
}
