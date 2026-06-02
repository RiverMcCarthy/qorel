using System.Collections.Frozen;

using Qorel.Fields;
using Qorel.Filters;
using Qorel.Results;

namespace Qorel.Schema;

/// <summary>
/// Abstract base class that declares which fields clients may filter and sort.
/// Subclass once per entity, override <see cref="Define"/>, and register via
/// <see cref="ServiceCollectionExtensions.AddQorelSchema{TSchema,TKey}"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Define"/> is called lazily on first access. Each call to <see cref="For{TProp}"/>
/// inside <see cref="Define"/> registers a filter parser on this schema instance.
/// Two schemas with the same enum field type are fully isolated.
/// </para>
/// <para>
/// The <c>QueryPipeline</c> calls <see cref="EnsureInitialised"/> in its constructor, so any
/// misconfiguration is caught at startup.
/// </para>
/// </remarks>
/// <typeparam name="TKey">
/// Enum type whose members contain the available fields.
/// </typeparam>
public abstract class FieldSchema<TKey> where TKey : Enum
{
    private readonly Lazy<FrozenDictionary<TKey, FieldMetadata<TKey>>> _map;

    // Populated by For<TProp>() during Define().
    // Written once inside the lazy lock, then read-only.
    private readonly Dictionary<Type, IFilterValueParser> _enumParsers = [];

    /// <summary>
    /// Initialises the schema with lazy field map construction.
    /// </summary>
    protected FieldSchema()
    {
        _map = new Lazy<FrozenDictionary<TKey, FieldMetadata<TKey>>>(
            () => Define().ToFrozenDictionary(m => m.Field.Key));
    }

    /// <summary>
    /// Override to declare metadata for every field available to clients.
    /// Use <see cref="For{TProp}"/> to start each entry's fluent builder chain.
    /// </summary>
    protected abstract IEnumerable<FieldMetadata<TKey>> Define();

    /// <summary>
    /// Returns the metadata for the specified field.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when <paramref name="field"/> is not registered.</exception>
    public FieldMetadata<TKey> Get(TKey field)
    {
        if (!_map.Value.TryGetValue(field, out FieldMetadata<TKey>? metadata))
            throw new KeyNotFoundException($"Field '{field}' is not registered in {GetType().Name}.");

        return metadata;
    }

    /// <summary>
    /// Tries to retrieve the metadata for the specified field.
    /// Returns <c>false</c> when <paramref name="field"/> is not registered.
    /// </summary>
    public bool TryGet(TKey field, out FieldMetadata<TKey> metadata)
        => _map.Value.TryGetValue(field, out metadata!);

    /// <summary>
    /// Returns all registered field metadata entries.
    /// </summary>
    public IReadOnlyCollection<FieldMetadata<TKey>> GetAll() => _map.Value.Values;

    /// <summary>
    /// Converts raw <see cref="FilterRequest{TKey}"/> values into validated,
    /// strongly-typed <see cref="IFilter{TKey}"/> instances.
    /// </summary>
    /// <remarks>
    /// Use this when building a custom query that bypasses <c>QueryPipeline</c>.
    /// </remarks>
    public QueryResult<IReadOnlyList<IFilter<TKey>>> ParseFilters(
        IEnumerable<FilterRequest<TKey>> requests)
    {
        var filters = new List<IFilter<TKey>>();
        var errors = new List<ValidationError>();

        foreach (FilterRequest<TKey> request in requests)
        {
            if (!TryGet(request.FieldKey, out FieldMetadata<TKey>? metadata))
            {
                errors.Add(ValidationError.InvalidField(request.FieldKey));
                continue;
            }

            if (!metadata.IsFilterable)
            {
                errors.Add(ValidationError.FieldNotFilterable(request.FieldKey));
                continue;
            }

            if (!metadata.AllowedOperators.Contains(request.Operator))
            {
                errors.Add(ValidationError.OperatorNotAllowed(request.FieldKey, request.Operator));
                continue;
            }

            filters.Add(GetParser(metadata.Type)
                .Build(metadata.Field, request.Operator, request.Value));
        }

        return errors.Count > 0
            ? QueryResult<IReadOnlyList<IFilter<TKey>>>.Failure(errors)
            : QueryResult<IReadOnlyList<IFilter<TKey>>>.Success(filters.AsReadOnly());
    }

    /// <summary>
    /// Returns the filter value parser for <paramref name="type"/>.
    /// </summary>
    internal IFilterValueParser GetParser(Type type)
    {
        Type t = Nullable.GetUnderlyingType(type) ?? type;

        if (FilterFactory.BuiltInParsers.TryGetValue(t, out IFilterValueParser? parser))
            return parser;

        // Accessing value here ensures the parser exists
        _ = _map.Value;

        if (_enumParsers.TryGetValue(t, out parser))
            return parser;

        throw new InvalidOperationException(
            $"No filter parser registered for type '{t.Name}' in {GetType().Name}.");
    }

    /// <summary>
    /// Triggers initialisation of the schema to ensure configuration is correct at startup
    /// </summary>
    internal void EnsureInitialised() => _ = _map.Value;

    /// <summary>
    /// Factory method for starting a fluent <see cref="FieldBuilder{TKey,TProp}"/> chain.
    /// Registers the filter parser for <typeparamref name="TProp"/> on this schema instance.
    /// Enum types are registered here via the compile-time generic parameter.
    /// </summary>
    protected FieldBuilder<TKey, TProp> For<TProp>(Field<TKey, TProp> field)
    {
        Type underlying = Nullable.GetUnderlyingType(typeof(TProp)) ?? typeof(TProp);

        if (underlying.IsEnum)
            _enumParsers.TryAdd(underlying, FilterFactory.CreateEnumParser<TProp>());

        return new FieldBuilder<TKey, TProp>(field);
    }
}
