using System.Linq.Expressions;

using Qorel.Filters;
using Qorel.Results;

namespace Qorel.EntityFramework.Pipeline;

/// <summary>
/// Validates and executes filter/sort/pagination queries for <typeparamref name="TEntity"/>,
/// returning cursor-paginated, projected results.
/// </summary>
/// <typeparam name="TEntity">The EF Core entity type.</typeparam>
/// <typeparam name="TKey">Enum identifying the filterable/sortable fields.</typeparam>
public interface IQueryPipeline<TEntity, TKey>
    where TEntity : class
    where TKey : Enum
{
    /// <summary>
    /// Validates and executes a client <see cref="QueryRequest{TKey}"/>, returning a cursor-paginated result.
    /// </summary>
    Task<QueryResult<Page<TResult>>> QueryAsync<TResult>(
        QueryRequest<TKey> request,
        Expression<Func<TEntity, TResult>> project,
        IReadOnlyList<IFilter<TKey>>? serverFilters = null,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a programmatically-built <see cref="QueryBuilder{TKey}"/>, returning a
    /// cursor-paginated result. Use this in services, background jobs, or tests where
    /// filters are already strongly typed.
    /// </summary>
    Task<QueryResult<Page<TResult>>> QueryAsync<TResult>(
        QueryBuilder<TKey> query,
        Expression<Func<TEntity, TResult>> project,
        IReadOnlyList<IFilter<TKey>>? serverFilters = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a single entity projected to <typeparamref name="TResult"/> by its primary key, or null if not found.
    /// </summary>
    Task<TResult?> FindAsync<TId, TResult>(
        TId id,
        Expression<Func<TEntity, TResult>> project,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all results matching the given filters without pagination.
    /// Use only for bounded result set.
    /// </summary>
    Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        IReadOnlyList<IFilter<TKey>> filters,
        Expression<Func<TEntity, TResult>> project,
        CancellationToken ct = default);
}
