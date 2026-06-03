using Qorel.Results;

namespace Qorel.Exceptions;

/// <summary>
/// Thrown when a pagination cursor cannot be decoded because it is malformed, expired, or has been tampered with.
/// </summary>
/// <remarks>
/// <c>QueryPipeline</c> catches this internally and converts it to a structured <see cref="ValidationError"/>
/// Custom pipelines that handle cursors directly should catch this type explicitly.
/// </remarks>
public sealed class InvalidCursorException : Exception
{
    /// <summary>
    /// Initialises a new instance with a default message.
    /// </summary>
    public InvalidCursorException()
        : base("The pagination cursor is invalid or has been tampered with.") { }

    /// <summary>
    /// Initialises a new instance with a custom message.
    /// </summary>
    public InvalidCursorException(string message)
        : base(message) { }

    /// <summary>
    /// Initialises a new instance with a custom message and inner exception.
    /// </summary>
    public InvalidCursorException(string message, Exception innerException)
        : base(message, innerException) { }
}
