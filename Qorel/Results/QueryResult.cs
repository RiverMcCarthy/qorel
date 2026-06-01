namespace Qorel.Results;

/// <summary>
/// The result of a Qorel query - either a success containing <see cref="Value"/>,
/// or a failure containing one or more <see cref="ValidationError"/> entries.
/// </summary>
/// <typeparam name="T">The value type on success.</typeparam>
public sealed class QueryResult<T>
{
    private readonly T? _value;

    /// <summary>
    /// True if the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// True if the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Validation errors. Empty on success.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// The result value. Only valid when <see cref="IsSuccess"/> is <c>true</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed on a failed result.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed QueryResult.");

    private QueryResult(T value)
    {
        IsSuccess = true;
        _value = value;
        Errors = [];
    }

    private QueryResult(IReadOnlyList<ValidationError> errors)
    {
        IsSuccess = false;
        Errors = errors;
    }

    /// <summary>
    /// Creates a successful result wrapping <paramref name="value"/>.
    /// </summary>
    public static QueryResult<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result from the given errors.
    /// </summary>
    public static QueryResult<T> Failure(params ValidationError[] errors) => new([.. errors]);

    /// <summary>
    /// Creates a failed result from an enumerable of errors.
    /// </summary>
    public static QueryResult<T> Failure(IEnumerable<ValidationError> errors) => new([.. errors]);

    /// <summary>
    /// Projects the success value to a new type, or propagates failure unchanged.
    /// </summary>
    public QueryResult<TOut> Map<TOut>(Func<T, TOut> map)
        => IsSuccess ? QueryResult<TOut>.Success(map(_value!)) : QueryResult<TOut>.Failure(Errors);

    /// <summary>
    /// Executes <paramref name="onSuccess"/> or <paramref name="onFailure"/> based on the result state.
    /// </summary>
    public TOut Match<TOut>(
        Func<T, TOut> onSuccess,
        Func<IReadOnlyList<ValidationError>, TOut> onFailure)
        => IsSuccess ? onSuccess(_value!) : onFailure(Errors);

    /// <summary>
    /// Returns the value on success, or throws <see cref="InvalidOperationException"/> on failure.
    /// </summary>
    public T ThrowIfInvalid()
    {
        if (IsFailure)
        {
            var message = string.Join("; ", Errors.Select(e => e.Message));
            throw new InvalidOperationException($"Query failed: {message}");
        }

        return _value!;
    }
}
