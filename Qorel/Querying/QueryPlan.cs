using System.Linq.Expressions;

namespace Qorel;

/// <summary>
/// A query that has been checked and is ready to run against any <see cref="IQueryable{T}"/>: what matches, and how to
/// page through it. Made by <see cref="QueryPlanner{TEntity,TField}"/>.
/// </summary>
internal sealed class QueryPlan<TEntity, TField>(
    BuiltModel<TEntity, TField> model,
    IReadOnlyList<Filter<TField>> filters,
    string? search,
    BoundingBox? spatialBounds,
    KeysetPagination<TEntity, TField> pagination)
    where TEntity : class
    where TField : struct, Enum
{
    /// <summary>
    /// Narrows <paramref name="source"/> to the matching items: every filter, the search, and the spatial bounds.
    /// Pages and counts both start here, so they always agree.
    /// </summary>
    public IQueryable<TEntity> Matching(IQueryable<TEntity> source)
    {
        IQueryable<TEntity> matching = model.ApplyFilters(source, filters);

        if (TextSearch.Build(model.SearchFields, search) is { } searchPredicate)
            matching = matching.Where(searchPredicate);

        if (spatialBounds is { } bounds && model.SpatialFilter is { } spatialFilter)
            matching = matching.Where(spatialFilter(bounds));

        return matching;
    }

    /// <summary>
    /// The query for the page, ready for the caller to run. Pass the rows it returns to <see cref="ToPage{TResult}"/>.
    /// </summary>
    public IQueryable<PageRow<TResult>> SelectPage<TResult>(IQueryable<TEntity> source, Expression<Func<TEntity, TResult>> projection)
        => pagination.SelectPage(Matching(source), projection);

    /// <summary>
    /// Turns the rows <see cref="SelectPage{TResult}"/> read into a page with its next cursor.
    /// </summary>
    public Page<TResult> ToPage<TResult>(IReadOnlyList<PageRow<TResult>> rows) => pagination.ToPage(rows);
}
