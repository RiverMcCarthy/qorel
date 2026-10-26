namespace Qorel;

/// <summary>
/// Declares how <typeparamref name="TEntity"/> can be queried: which fields exist, how each is read from the entity, and
/// which of them clients may filter, sort, and search by. Subclass it once per entity and override <see cref="Configure"/>.
/// </summary>
/// <example>
/// <code>
/// public sealed class ProductQueryModel : QueryModel&lt;Product, ProductField&gt;
/// {
///     protected override void Configure(QueryModelBuilder&lt;Product, ProductField&gt; model)
///     {
///         model.Id(ProductFields.Id, e =&gt; e.Id);
///         model.Map(ProductFields.Name, e =&gt; e.Name).Filterable().Sortable().Searchable().IgnoreCase();
///         model.Map(ProductFields.Price, e =&gt; e.Price).Filterable().Sortable();
///         model.Map(ProductFields.CreatedAt, e =&gt; e.CreatedAt).Sortable();
///         model.Map(ProductFields.OwnerId, e =&gt; e.OwnerId); // server-side filters only
///         model.DefaultSort(ProductFields.CreatedAt, SortDirection.Desc);
///     }
/// }
/// </code>
/// </example>
/// <remarks>
/// <see cref="Configure"/> runs once. Register the model with <see cref="QorelBuilder.AddModel{TEntity,TField}"/>, which
/// runs it at startup so mistakes surface straight away. A model that is not registered, e.g. one created to filter data
/// in memory, runs it on first use with the default <see cref="QorelOptions"/>. A model is immutable once built.
/// </remarks>
/// <typeparam name="TEntity">The entity type, e.g. an EF Core entity or an in-memory record.</typeparam>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
public abstract class QueryModel<TEntity, TField>
    where TEntity : class
    where TField : struct, Enum
{
    private readonly Lazy<Compiled> _compiled;
    private QorelOptions _options = new();

    /// <summary>
    /// Creates the model. <see cref="Configure"/> runs later, when the model is registered or first used.
    /// </summary>
    protected QueryModel() => _compiled = new(Compile);

    /// <summary>
    /// The fields clients can filter, sort, or search by. Fields mapped only for server-side use are not included.
    /// </summary>
    public IReadOnlyList<ClientField<TField>> ClientFields => Built.ClientFields;

    internal BuiltModel<TEntity, TField> Built => _compiled.Value.Model;

    private RequestValidator<TEntity, TField> Validator => _compiled.Value.Validator;

    private QueryPlanner<TEntity, TField> Planner => _compiled.Value.Planner;

    /// <summary>
    /// Checks client filters against the model and turns them into typed filters. Use it in a custom query that
    /// does not go through <see cref="IQueryPipeline{TEntity,TField}"/>, so client input is still validated.
    /// </summary>
    /// <returns>The filters as a <see cref="FilterSet{TField}"/>, or every validation error.</returns>
    public QueryResult<FilterSet<TField>> ParseFilters(IEnumerable<FilterRequest<TField>> requests)
        => Validator.ParseFilters(requests);

    /// <summary>
    /// Reads a JSON array of filters and checks them, e.g. from a single <c>filters</c> query string parameter.
    /// <c>null</c>, blank, or a JSON <c>null</c> means no filters.
    /// </summary>
    /// <returns>
    /// The filters as a <see cref="FilterSet{TField}"/>, a <see cref="ErrorCodes.FiltersMalformed"/> error when the JSON
    /// cannot be read, or every validation error.
    /// </returns>
    public QueryResult<FilterSet<TField>> ParseFilters(string? json)
        => Validator.ParseFilters(json);

    /// <summary>
    /// Adds <paramref name="filters"/> to a query, for a LINQ provider such as EF Core to run.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a filter's field is not mapped.</exception>
    public IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> source, IEnumerable<Filter<TField>> filters)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(filters);

        return Built.ApplyFilters(source, filters);
    }

    /// <summary>
    /// Filters data that is already in memory with the same filters you would send to the database.
    /// </summary>
    /// <remarks>
    /// Results match the database's, except where its collation decides: text is compared ordinally here, so it is
    /// case-sensitive unless the field is mapped with <c>IgnoreCase()</c>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when a filter's field is not mapped.</exception>
    public IEnumerable<TEntity> ApplyFilters(IEnumerable<TEntity> source, IEnumerable<Filter<TField>> filters)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(filters);

        return Built.ApplyFilters(source, filters);
    }

    /// <summary>
    /// Declares the model's fields. Map the id with <see cref="QueryModelBuilder{TEntity,TField}.Id{TValue}"/> and every
    /// other field with <see cref="QueryModelBuilder{TEntity,TField}.Map{TValue}"/>.
    /// </summary>
    protected abstract void Configure(QueryModelBuilder<TEntity, TField> model);

    /// <summary>
    /// Builds the model with <paramref name="options"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the model has already been built.</exception>
    internal void Initialize(QorelOptions options)
    {
        if (_compiled.IsValueCreated)
            throw new InvalidOperationException($"{GetType().Name} has already been built. Register a model before using it, and only once.");

        _options = options;
        _ = _compiled.Value;
    }

    /// <summary>
    /// Checks a client's request and plans it, with <paramref name="serverFilters"/> added. See <see cref="QueryPlanner{TEntity,TField}"/>.
    /// </summary>
    internal QueryResult<QueryPlan<TEntity, TField>> Plan(QueryRequest<TField> request, IEnumerable<Filter<TField>> serverFilters)
        => Planner.Plan(request, serverFilters);

    /// <summary>
    /// Plans a query built in code, with <paramref name="serverFilters"/> added.
    /// </summary>
    internal QueryResult<QueryPlan<TEntity, TField>> Plan(QueryBuilder<TField> query, IEnumerable<Filter<TField>> serverFilters)
        => Planner.Plan(query, serverFilters);

    /// <summary>
    /// A filter matching the item with <paramref name="id"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <typeparamref name="TId"/> is not the id's type.</exception>
    internal Filter<TField> FilterById<TId>(TId id) => Built.Id.CreateEqualsFilter(id);

    // Runs Configure and sets up everything that works from the result. Lazy<T> makes sure this only happens once.
    private Compiled Compile()
    {
        var builder = new QueryModelBuilder<TEntity, TField>(GetType().Name);
        Configure(builder);

        BuiltModel<TEntity, TField> model = builder.Build(_options);
        var validator = new RequestValidator<TEntity, TField>(model);
        return new Compiled(model, validator, new QueryPlanner<TEntity, TField>(model, validator));
    }

    private sealed record Compiled(
        BuiltModel<TEntity, TField> Model,
        RequestValidator<TEntity, TField> Validator,
        QueryPlanner<TEntity, TField> Planner);
}
