using System.Linq.Expressions;

namespace Qorel;

/// <summary>
/// Runs queries against a <see cref="QueryModel{TEntity,TField}"/>: validates client requests, then filters, sorts, and
/// pages <typeparamref name="TEntity"/>, projecting each item to the shape you ask for.
/// </summary>
/// <remarks>
/// <para>
/// Inject this interface rather than a backend's implementation, so handlers do not depend on the data store and can be
/// tested with a fake. Register a backend such as <c>AddEntityFramework</c> to get one.
/// </para>
/// <para>
/// Every method applies the <see cref="IServerFilter{TEntity}"/>s registered for the entity. Use the
/// <c>additionalFilters</c> parameter when you need an extra filter for just one call.
/// </para>
/// </remarks>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
public interface IQueryPipeline<TEntity, TField>
    where TEntity : class
    where TField : struct, Enum
{
    /// <summary>
    /// Validates a client's query and returns the page it asks for.
    /// </summary>
    /// <param name="request">The client's query.</param>
    /// <param name="projection">Shapes each item, e.g. <c>e =&gt; new ProductDto(e.Id, e.Name)</c>. It runs in the database.</param>
    /// <param name="additionalFilters">
    /// Extra filters for this call only, added on top of the client's. The client can't see or remove them, and they
    /// can use any mapped field, even ones clients can't. If every query needs the filter, register an
    /// <see cref="IServerFilter{TEntity}"/> instead.
    /// </param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The page, or the validation errors if the request is invalid.</returns>
    Task<QueryResult<Page<TResult>>> QueryAsync<TResult>(
        QueryRequest<TField> request,
        Expression<Func<TEntity, TResult>> projection,
        IEnumerable<Filter<TField>>? additionalFilters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the page a query built in code asks for. Add every filter it needs to the query with
    /// <see cref="QueryBuilder{TField}.Where(Filter{TField}[])"/>.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="projection">Shapes each item. It runs in the database.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The page, or an error if the cursor or spatial bounds are invalid.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the query uses a field the model does not map.</exception>
    Task<QueryResult<Page<TResult>>> QueryAsync<TResult>(
        QueryBuilder<TField> query,
        Expression<Func<TEntity, TResult>> projection,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a client's query and counts the items that match its filters and spatial bounds. Its sorts, cursor, and
    /// page size are ignored, so a client can send the same query it pages with.
    /// </summary>
    /// <param name="request">The client's query.</param>
    /// <param name="additionalFilters">Filters added on top of the client's, as for <c>QueryAsync</c>.</param>
    /// <param name="maxCount">
    /// Stops counting once more than this many items match, which keeps counts cheap on big tables.
    /// </param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxCount"/> is less than 1 or is <see cref="int.MaxValue"/>.</exception>
    Task<QueryResult<ItemCount>> CountAsync(
        QueryRequest<TField> request,
        IEnumerable<Filter<TField>>? additionalFilters = null,
        int? maxCount = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the items that match a query built in code. Its sorts, cursor, and page size are ignored.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="maxCount">Stops counting once more than this many items match.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxCount"/> is less than 1 or is <see cref="int.MaxValue"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the query uses a field the model does not map.</exception>
    Task<QueryResult<ItemCount>> CountAsync(
        QueryBuilder<TField> query,
        int? maxCount = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds one item by its id, or returns the default when there is none.
    /// </summary>
    /// <param name="id">The id. It must be the type the model's id is mapped as.</param>
    /// <param name="projection">Shapes the item. It runs in the database.</param>
    /// <param name="additionalFilters">Extra filters for this call only. The item has to match them too.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <exception cref="ArgumentException">Thrown when <typeparamref name="TId"/> is not the id's type.</exception>
    Task<TResult?> FindAsync<TId, TResult>(
        TId id,
        Expression<Func<TEntity, TResult>> projection,
        IEnumerable<Filter<TField>>? additionalFilters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every item that matches <paramref name="filters"/>, unpaged. Only use it where the number of matches is
    /// known to be small.
    /// </summary>
    /// <param name="filters">The filters. They can use any mapped field.</param>
    /// <param name="projection">Shapes each item. It runs in the database.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <exception cref="InvalidOperationException">Thrown when a filter uses a field the model does not map.</exception>
    Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        IEnumerable<Filter<TField>> filters,
        Expression<Func<TEntity, TResult>> projection,
        CancellationToken cancellationToken = default);
}
