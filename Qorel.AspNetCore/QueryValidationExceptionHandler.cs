using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Qorel;

/// <summary>
/// Turns a <see cref="QueryValidationException"/>, thrown by <see cref="QueryResult{T}.GetValueOrThrow"/>, into the
/// same <see cref="QueryValidationProblem"/> response <c>ToHttpResult()</c> returns, rather than a 500.
/// </summary>
/// <example>
/// <code>
/// builder.Services.AddProblemDetails();
/// builder.Services.AddExceptionHandler&lt;QueryValidationExceptionHandler&gt;();
/// …
/// app.UseExceptionHandler();
/// </code>
/// </example>
public sealed class QueryValidationExceptionHandler : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is not QueryValidationException invalid)
            return false;

        await new QueryValidationProblem(invalid.Errors).ExecuteAsync(httpContext).ConfigureAwait(false);
        return true;
    }
}
