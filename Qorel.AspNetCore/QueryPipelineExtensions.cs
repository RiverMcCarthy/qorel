using System.Linq.Expressions;

namespace Qorel;

/// <summary>
/// Runs queries sent as <see cref="QueryParameters{TField}"/>, for GET endpoints.
/// </summary>
public static class QueryPipelineExtensions
{
    /// <summary>
    /// Reads <paramref name="parameters"/> into a request, then runs it like
    /// <see cref="IQueryPipeline{TEntity,TField}.QueryAsync{TResult}(QueryRequest{TField}, Expression{Func{TEntity, TResult}}, IEnumerable{Filter{TField}}?, CancellationToken)"/>.
    /// </summary>
    public static async Task<QueryResult<Page<TResult>>> QueryAsync<TEntity, TField, TResult>(
        this IQueryPipeline<TEntity, TField> pipeline,
        QueryParameters<TField> parameters,
        Expression<Func<TEntity, TResult>> projection,
        IEnumerable<Filter<TField>>? serverFilters = null,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TField : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(parameters);

        QueryResult<QueryRequest<TField>> request = parameters.ToRequest();

        return request.IsSuccess
            ? await pipeline.QueryAsync(request.Value, projection, serverFilters, cancellationToken).ConfigureAwait(false)
            : QueryResult<Page<TResult>>.Failure(request.Errors);
    }

    /// <summary>
    /// Reads <paramref name="parameters"/> into a request, then counts it like
    /// <see cref="IQueryPipeline{TEntity,TField}.CountAsync(QueryRequest{TField}, IEnumerable{Filter{TField}}?, int?, CancellationToken)"/>.
    /// </summary>
    public static async Task<QueryResult<ItemCount>> CountAsync<TEntity, TField>(
        this IQueryPipeline<TEntity, TField> pipeline,
        QueryParameters<TField> parameters,
        IEnumerable<Filter<TField>>? serverFilters = null,
        int? maxCount = null,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TField : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(parameters);

        QueryResult<QueryRequest<TField>> request = parameters.ToRequest();

        return request.IsSuccess
            ? await pipeline.CountAsync(request.Value, serverFilters, maxCount, cancellationToken).ConfigureAwait(false)
            : QueryResult<ItemCount>.Failure(request.Errors);
    }
}
