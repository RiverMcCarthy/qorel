namespace Qorel;

/// <summary>
/// Thrown by <see cref="QueryResult{T}.GetValueOrThrow"/> when the result is a failure.
/// </summary>
public sealed class QueryValidationException : Exception
{
    /// <summary>
    /// Creates an exception with no errors.
    /// </summary>
    public QueryValidationException()
        : this([]) { }

    /// <summary>
    /// Creates an exception with a message and no errors.
    /// </summary>
    public QueryValidationException(string message)
        : base(message) => Errors = [];

    /// <summary>
    /// Creates an exception with a message, an inner exception, and no errors.
    /// </summary>
    public QueryValidationException(string message, Exception innerException)
        : base(message, innerException) => Errors = [];

    /// <summary>
    /// Creates an exception for <paramref name="errors"/>. The message lists them all.
    /// </summary>
    public QueryValidationException(IReadOnlyList<ValidationError> errors)
        : base($"The query is invalid: {string.Join("; ", errors.Select(error => error.Message))}")
        => Errors = errors;

    /// <summary>
    /// What was wrong with the query.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }
}
