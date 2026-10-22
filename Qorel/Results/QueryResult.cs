namespace Qorel;

/// <summary>
/// Either a value, or the validation errors that stopped it being produced. Client mistakes come back this way rather
/// than as exceptions, so they can be mapped to a 4xx response.
/// </summary>
/// <typeparam name="T">The value on success.</typeparam>
public sealed class QueryResult<T>
{
    private readonly T? _value;

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
    /// True when there is a <see cref="Value"/>.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// True when there are <see cref="Errors"/>.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// What went wrong. Empty on success.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// The value. Check <see cref="IsSuccess"/> first.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed QueryResult has no value. Check IsSuccess first.");

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static QueryResult<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static QueryResult<T> Failure(params ValidationError[] errors) => Failure((IEnumerable<ValidationError>)errors);

    /// <inheritdoc cref="Failure(ValidationError[])"/>
    /// <exception cref="ArgumentException">Thrown when there are no errors.</exception>
    public static QueryResult<T> Failure(IEnumerable<ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        ValidationError[] all = [.. errors];
        return all.Length > 0 ? new(all) : throw new ArgumentException("A failed result needs at least one error.", nameof(errors));
    }

    /// <summary>
    /// Transforms the value, or passes the errors through.
    /// </summary>
    public QueryResult<TOut> Map<TOut>(Func<T, TOut> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return IsSuccess ? QueryResult<TOut>.Success(map(_value!)) : QueryResult<TOut>.Failure(Errors);
    }

    /// <summary>
    /// Runs a next step that can also fail, or passes the errors through.
    /// </summary>
    public QueryResult<TOut> Bind<TOut>(Func<T, QueryResult<TOut>> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return IsSuccess ? next(_value!) : QueryResult<TOut>.Failure(Errors);
    }

    /// <summary>
    /// Calls <paramref name="onSuccess"/> or <paramref name="onFailure"/>, and returns what it returns.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<ValidationError>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(_value!) : onFailure(Errors);
    }

    /// <summary>
    /// Returns the value, or throws when the result is a failure. For code that treats invalid input as a bug.
    /// </summary>
    /// <exception cref="QueryValidationException">Thrown when the result is a failure. It carries the errors.</exception>
    public T GetValueOrThrow() => IsSuccess ? _value! : throw new QueryValidationException(Errors);
}
