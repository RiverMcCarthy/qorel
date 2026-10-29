namespace Qorel;

/// <summary>
/// Checks a client's <see cref="QueryRequest{TField}"/> or a <see cref="QueryBuilder{TField}"/> and turns it into a
/// <see cref="QueryPlan{TEntity,TField}"/>. Client input is only checked against the model here and in
/// <see cref="RequestValidator{TEntity,TField}"/>, so nothing after the plan has to check it again.
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

    // Works out the sort keys and where the page starts. Fails when the cursor can't be read or was made for different sorts.
    private QueryResult<KeysetPagination<TEntity, TField>> Paginate(IReadOnlyList<Sort<TField>> sorts, string? cursor, int pageSize)
    {
        IReadOnlyList<SortKey<TEntity, TField>> keys = SortKeys(sorts);

        if (string.IsNullOrEmpty(cursor))
            return QueryResult<KeysetPagination<TEntity, TField>>.Success(new(keys, previousPageEnd: null, pageSize));

        return Cursor.TryDecode(cursor, keys, model.Options.MaxCursorLength, out object?[] previousPageEnd)
            ? QueryResult<KeysetPagination<TEntity, TField>>.Success(new(keys, previousPageEnd, pageSize))
            : QueryResult<KeysetPagination<TEntity, TField>>.Failure(ValidationError.CursorInvalid());
    }

    // The query's sorts, or the model's default sort when it has none. The id goes last, unless it's already a sort, so
    // no two items tie and nothing is skipped or repeated between pages.
    private List<SortKey<TEntity, TField>> SortKeys(IReadOnlyList<Sort<TField>> sorts)
    {
        IReadOnlyList<Sort<TField>> chosen = sorts.Count > 0 ? sorts : model.DefaultSorts;
        List<SortKey<TEntity, TField>> keys = [.. chosen.Select(sort => new SortKey<TEntity, TField>(model.GetField(sort.Field), sort.Direction))];

        if (!keys.Exists(key => key.Field == model.Id))
            keys.Add(new SortKey<TEntity, TField>(model.Id, SortDirection.Asc));

        return keys;
    }
}
