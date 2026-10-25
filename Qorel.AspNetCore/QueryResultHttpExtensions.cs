using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Qorel;

/// <summary>
/// Turns a <see cref="QueryResult{T}"/> into an HTTP response.
/// </summary>
/// <example>
/// <code>
/// app.MapPost("/products/query", async (QueryRequest&lt;ProductField&gt; request, IQueryPipeline&lt;Product, ProductField&gt; pipeline) =&gt;
///     (await pipeline.QueryAsync(request, e =&gt; new ProductDto(e.Id, e.Name))).ToHttpResult());
/// </code>
/// </example>
public static class QueryResultHttpExtensions
{
    /// <summary>
    /// Returns <c>200 OK</c> with the value, or a <see cref="QueryValidationProblem"/> listing every error.
    /// </summary>
    /// <remarks>
    /// The return type tells OpenAPI about both responses, so a minimal API endpoint that returns this is documented
    /// automatically. Controllers can return it as an <see cref="IResult"/>.
    /// </remarks>
    public static Results<Ok<T>, QueryValidationProblem> ToHttpResult<T>(this QueryResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : new QueryValidationProblem(result.Errors);
    }

    /// <summary>
    /// Returns what <paramref name="onSuccess"/> makes of the value, or a <see cref="QueryValidationProblem"/> listing
    /// every error. Use it when success is not a plain <c>200 OK</c>, e.g. a file download.
    /// </summary>
    public static Results<TSuccess, QueryValidationProblem> ToHttpResult<T, TSuccess>(this QueryResult<T> result, Func<T, TSuccess> onSuccess)
        where TSuccess : IResult
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result.IsSuccess ? onSuccess(result.Value) : new QueryValidationProblem(result.Errors);
    }
}
