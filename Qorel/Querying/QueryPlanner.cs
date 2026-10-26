namespace Qorel;

/// <summary>
/// Turns a client's <see cref="QueryRequest{TField}"/> or a <see cref="QueryBuilder{TField}"/> into a
/// <see cref="QueryPlan{TEntity,TField}"/>.
/// </summary>
internal sealed class QueryPlanner<TEntity, TField>(BuiltModel<TEntity, TField> model, RequestValidator<TEntity, TField> validator)
    where TEntity : class
    where TField : struct, Enum
{
    /// <summary>
    /// Checks every part of a client's request, and plans it with <paramref name="additionalFilters"/> added. Returns
    /// every problem at once, so a client can fix them all in one go.
    /// </summary>
    public QueryResult<QueryPlan<TEntity, TField>> Plan(QueryRequest<TField> request, IEnumerable<Filter<TField>> additionalFilters)
    {
        ArgumentNullException.ThrowIfNull(request);

        // A JSON null for either list arrives as null, despite the default.
        QueryResult<FilterSet<TField>> filters = validator.ParseFilters(request.Filters ?? []);
        QueryResult<KeysetPagination<TEntity, TField>> pagination = validator
            .ParseSorts(request.Sorts ?? [])
            .Bind(sorts => Paginate(sorts, request.Cursor, validator.ResolvePageSize(request.PageSize)));

        ValidationError[] errors =
        [
            .. filters.Errors,
            .. pagination.Errors,
            .. validator.CheckSearch(request.Search),
            .. validator.CheckSpatialBounds(request.SpatialBounds),
        ];

        if (errors.Length > 0)
            return QueryResult<QueryPlan<TEntity, TField>>.Failure(errors);

        return QueryResult<QueryPlan<TEntity, TField>>.Success(
            new(model, [.. filters.Value, .. additionalFilters], request.Search, request.SpatialBounds, pagination.Value));
    }

    /// <summary>
    /// Plans a query built in code. It is not held to the client rules, so only the cursor and spatial bounds can be invalid.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the query uses a field the model does not map, or searches a model with no searchable fields.
    /// Both are bugs in the calling code rather than bad input.
    /// </exception>
    public QueryResult<QueryPlan<TEntity, TField>> Plan(QueryBuilder<TField> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        ThrowIfNotMapped(query.Filters);

        if (!string.IsNullOrWhiteSpace(query.Search) && model.SearchFields.Count == 0)
            throw new InvalidOperationException($"{model.Name} has no searchable fields. Mark some with Searchable() to search it.");

        // This throws for an unmapped sort field, since it looks up every sort's field.
        QueryResult<KeysetPagination<TEntity, TField>> pagination =
            Paginate(query.Sorts, query.Cursor, query.PageSize ?? model.DefaultPageSize);

        ValidationError[] errors = [.. pagination.Errors, .. validator.CheckSpatialBounds(query.SpatialBounds)];

        if (errors.Length > 0)
            return QueryResult<QueryPlan<TEntity, TField>>.Failure(errors);

        return QueryResult<QueryPlan<TEntity, TField>>.Success(
            new(model, [.. query.Filters], query.Search, query.SpatialBounds, pagination.Value));
    }

    // Filters only turn into predicates when the query runs. Checking them here means an unmapped field always throws.
    private void ThrowIfNotMapped(IEnumerable<Filter<TField>> filters)
    {
        foreach (Filter<TField> filter in filters)
            _ = model.GetField(filter.Field);
    }

    // A query with no sorts uses the model's default sort.
    private QueryResult<KeysetPagination<TEntity, TField>> Paginate(IReadOnlyList<Sort<TField>> sorts, string? cursor, int pageSize)
        => KeysetPagination<TEntity, TField>.Create(model, sorts.Count > 0 ? sorts : model.DefaultSorts, cursor, pageSize);
}
