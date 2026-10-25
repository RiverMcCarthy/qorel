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
    /// Checks every part of a client's request, and plans it with <paramref name="serverFilters"/> added. Returns every
    /// problem at once, so a client can fix them all in one go.
    /// </summary>
    public QueryResult<QueryPlan<TEntity, TField>> Plan(QueryRequest<TField> request, IEnumerable<Filter<TField>> serverFilters)
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
            new(model, [.. filters.Value, .. serverFilters], request.Search, request.SpatialBounds, pagination.Value));
    }

    /// <summary>
    /// Plans a query built in code, with <paramref name="serverFilters"/> added. It is not held to the client rules, so
    /// only the cursor and spatial bounds can be invalid.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the query uses a field the model does not map, or searches a model with no searchable fields.
    /// Both are bugs in the calling code rather than bad input.
    /// </exception>
    public QueryResult<QueryPlan<TEntity, TField>> Plan(QueryBuilder<TField> query, IEnumerable<Filter<TField>> serverFilters)
    {
        ArgumentNullException.ThrowIfNull(query);

        // GetField throws for a field the model does not map.
        foreach (TField field in query.Filters.Select(filter => filter.Field).Concat(query.Sorts.Select(sort => sort.Field)))
            _ = model.GetField(field);

        if (!string.IsNullOrWhiteSpace(query.Search) && model.SearchFields.Count == 0)
            throw new InvalidOperationException($"{model.Name} has no searchable fields. Mark some with Searchable() to search it.");

        QueryResult<KeysetPagination<TEntity, TField>> pagination =
            Paginate(query.Sorts, query.Cursor, query.PageSize ?? model.DefaultPageSize);

        ValidationError[] errors = [.. pagination.Errors, .. validator.CheckSpatialBounds(query.SpatialBounds)];

        if (errors.Length > 0)
            return QueryResult<QueryPlan<TEntity, TField>>.Failure(errors);

        return QueryResult<QueryPlan<TEntity, TField>>.Success(
            new(model, [.. query.Filters, .. serverFilters], query.Search, query.SpatialBounds, pagination.Value));
    }

    // A query with no sorts uses the model's default sort.
    private QueryResult<KeysetPagination<TEntity, TField>> Paginate(IReadOnlyList<Sort<TField>> sorts, string? cursor, int pageSize)
        => KeysetPagination<TEntity, TField>.Create(model, sorts.Count > 0 ? sorts : model.DefaultSorts, cursor, pageSize);
}
