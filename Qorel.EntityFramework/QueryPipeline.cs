using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

namespace Qorel.EntityFramework;

/// <summary>
/// Runs queries with EF Core. The model plans each query; this class adds the registered server filters, runs the plan against the
/// entity set, and shapes the result.
/// </summary>
/// <remarks>Scoped, like the <see cref="DbContext"/> it uses. Every query is read-only and untracked.</remarks>
internal sealed class QueryPipeline<TEntity, TField>(
    QueryModel<TEntity, TField> model,
    DbContextAccessor db,
    ServerFilters<TEntity, TField> registeredFilters)
    : IQueryPipeline<TEntity, TField>
    where TEntity : class
    where TField : struct, Enum
{
    private IQueryable<TEntity> Entities => db.Context.Set<TEntity>().AsNoTracking();

    public Task<QueryResult<Page<TResult>>> QueryAsync<TResult>(
        QueryRequest<TField> request,
        Expression<Func<TEntity, TResult>> projection,
        IEnumerable<Filter<TField>>? serverFilters = null,
        CancellationToken cancellationToken = default)
        => ReadPageAsync(model.Plan(request, registeredFilters.With(serverFilters)), projection, cancellationToken);

    public Task<QueryResult<Page<TResult>>> QueryAsync<TResult>(
        QueryBuilder<TField> query,
        Expression<Func<TEntity, TResult>> projection,
        IEnumerable<Filter<TField>>? serverFilters = null,
        CancellationToken cancellationToken = default)
        => ReadPageAsync(model.Plan(query, registeredFilters.With(serverFilters)), projection, cancellationToken);

    public Task<QueryResult<ItemCount>> CountAsync(
        QueryRequest<TField> request,
        IEnumerable<Filter<TField>>? serverFilters = null,
        int? maxCount = null,
        CancellationToken cancellationToken = default)
        => CountAsync(model.Plan(request, registeredFilters.With(serverFilters)), maxCount, cancellationToken);

    public Task<QueryResult<ItemCount>> CountAsync(
        QueryBuilder<TField> query,
        IEnumerable<Filter<TField>>? serverFilters = null,
        int? maxCount = null,
        CancellationToken cancellationToken = default)
        => CountAsync(model.Plan(query, registeredFilters.With(serverFilters)), maxCount, cancellationToken);

    public async Task<TResult?> FindAsync<TId, TResult>(
        TId id,
        Expression<Func<TEntity, TResult>> projection,
        IEnumerable<Filter<TField>>? serverFilters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);

        IEnumerable<Filter<TField>> filters = [model.FilterById(id), .. registeredFilters.With(serverFilters)];

        return await model.ApplyFilters(Entities, filters)
            .Select(projection)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        IEnumerable<Filter<TField>> filters,
        Expression<Func<TEntity, TResult>> projection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filters);
        ArgumentNullException.ThrowIfNull(projection);

        return await model.ApplyFilters(Entities, registeredFilters.With(filters))
            .Select(projection)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<QueryResult<Page<TResult>>> ReadPageAsync<TResult>(
        QueryResult<QueryPlan<TEntity, TField>> plan,
        Expression<Func<TEntity, TResult>> projection,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projection);

        if (plan.IsFailure)
            return QueryResult<Page<TResult>>.Failure(plan.Errors);

        List<PageRow<TResult>> rows = await plan.Value.SelectPage(Entities, projection).ToListAsync(cancellationToken).ConfigureAwait(false);
        return QueryResult<Page<TResult>>.Success(plan.Value.ToPage(rows));
    }

    private async Task<QueryResult<ItemCount>> CountAsync(
        QueryResult<QueryPlan<TEntity, TField>> plan,
        int? maxCount,
        CancellationToken cancellationToken)
    {
        // int.MaxValue is out because the count goes one past the cap.
        if (maxCount is < 1 or int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "The max count must be at least 1 and less than int.MaxValue.");

        if (plan.IsFailure)
            return QueryResult<ItemCount>.Failure(plan.Errors);

        IQueryable<TEntity> matching = plan.Value.Matching(Entities);

        if (maxCount is not { } cap)
            return QueryResult<ItemCount>.Success(new ItemCount(await matching.CountAsync(cancellationToken).ConfigureAwait(false)));

        // Counting one past the cap tells "exactly the cap" apart from "more than the cap".
        int count = await matching.Take(cap + 1).CountAsync(cancellationToken).ConfigureAwait(false);

        return QueryResult<ItemCount>.Success(count > cap ? new ItemCount(cap, IsCapped: true) : new ItemCount(count));
    }
}
