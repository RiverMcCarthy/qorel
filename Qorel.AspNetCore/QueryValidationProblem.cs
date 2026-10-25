using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Qorel;

/// <summary>
/// Returns a <c>422 Unprocessable Entity</c> problem details response when a query fails validation. Every
/// <see cref="ValidationError"/> goes in the <c>errors</c> array, so the client gets all of them in one go:
/// <code>
/// {
///   "type": "https://tools.ietf.org/html/rfc9110#section-15.5.21",
///   "title": "The query is invalid.",
///   "status": 422,
///   "errors": [{ "code": "field.not_sortable", "message": "Field 'Category' cannot be used in a sort.", "field": "Category" }]
/// }
/// </code>
/// </summary>
/// <remarks>
/// You normally get this from <see cref="QueryResultHttpExtensions.ToHttpResult{T}(QueryResult{T})"/> rather than
/// creating it yourself. It adds its own OpenAPI metadata, so you don't need a <c>.Produces(422)</c> on the endpoint.
/// </remarks>
public sealed class QueryValidationProblem : IResult, IStatusCodeHttpResult, IEndpointMetadataProvider
{
    private const string Type = "https://tools.ietf.org/html/rfc9110#section-15.5.21";
    private const string Title = "The query is invalid.";

    private readonly ProblemHttpResult _problem;

    /// <summary>
    /// Builds the 422 response from the validation errors.
    /// </summary>
    public QueryValidationProblem(IReadOnlyList<ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        Errors = errors;
        _problem = TypedResults.Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: Title,
            type: Type,
            extensions: new Dictionary<string, object?> { ["errors"] = errors });
    }

    /// <summary>
    /// The validation errors sent back to the client.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    int? IStatusCodeHttpResult.StatusCode => StatusCodes.Status422UnprocessableEntity;

    /// <summary>
    /// The problem details body. Handy in tests when you want to check the response without running it.
    /// </summary>
    public ProblemDetails ProblemDetails => _problem.ProblemDetails;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext) => _problem.ExecuteAsync(httpContext);

    /// <inheritdoc/>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Metadata.Add(new ProducesResponseTypeMetadata(
            StatusCodes.Status422UnprocessableEntity,
            typeof(ProblemDetails),
            ["application/problem+json"]));
    }
}
