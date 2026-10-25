using System.Text.Json;

namespace Qorel;

/// <summary>
/// Checks each part of a client's query against a model, and turns filters and sorts into their typed form.
/// <see cref="QueryPlanner{TEntity,TField}"/> puts the parts together.
/// </summary>
/// <remarks>
/// Every problem is collected rather than stopping at the first, so a client can fix them all at once.
/// Fields that are only for server-side use are reported as unknown, so a client cannot discover them.
/// </remarks>
internal sealed class RequestValidator<TEntity, TField>(BuiltModel<TEntity, TField> model)
    where TEntity : class
    where TField : struct, Enum
{
    private QorelOptions Options => model.Options;

    public QueryResult<FilterSet<TField>> ParseFilters(IEnumerable<FilterRequest<TField>?> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        FilterRequest<TField>?[] all = [.. requests];

        if (all.Length > Options.MaxFilters)
            return QueryResult<FilterSet<TField>>.Failure(ValidationError.TooManyFilters(Options.MaxFilters));

        return Collect(all.Select(ParseFilter)).Map(FilterSet<TField>.Create);
    }

    /// <summary>
    /// Reads a JSON array of filters, e.g. from a <c>filters</c> query string parameter, then validates them.
    /// <c>null</c>, blank, or a JSON <c>null</c> means no filters.
    /// </summary>
    public QueryResult<FilterSet<TField>> ParseFilters(string? json)
        => RequestJson.ReadFilters<TField>(json).Bind(ParseFilters);

    private QueryResult<Filter<TField>> ParseFilter(FilterRequest<TField>? request)
    {
        if (request is null)
            return Fail<Filter<TField>>(ValidationError.FiltersMalformed());

        if (model.FindClientField(request.Field) is not { } field)
            return Fail<Filter<TField>>(ValidationError.UnknownField(request.Field));

        if (!field.Client.IsFilterable)
            return Fail<Filter<TField>>(ValidationError.FieldNotFilterable(request.Field));

        if (!field.Client.AllowedOperators.Contains(request.Operator))
            return Fail<Filter<TField>>(ValidationError.OperatorNotAllowed(request.Field, request.Operator));

        // Checked before the values are read, so an oversized list is never read in full.
        if (request.Value.ValueKind == JsonValueKind.Array && request.Value.GetArrayLength() > Options.MaxFilterValues)
            return Fail<Filter<TField>>(ValidationError.TooManyFilterValues(request.Field, Options.MaxFilterValues));

        return field.TryCreateFilter(request.Operator, request.Value) is { } filter
            ? QueryResult<Filter<TField>>.Success(filter)
            : Fail<Filter<TField>>(ValidationError.FilterValueInvalid(request.Field, request.Operator));
    }

    public QueryResult<IReadOnlyList<Sort<TField>>> ParseSorts(IEnumerable<SortRequest<TField>?> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        SortRequest<TField>?[] all = [.. requests];

        if (all.Length > Options.MaxSorts)
            return QueryResult<IReadOnlyList<Sort<TField>>>.Failure(ValidationError.TooManySorts(Options.MaxSorts));

        HashSet<TField> seen = [];
        return Collect(all.Select(request => ParseSort(request, seen)));
    }

    private QueryResult<Sort<TField>> ParseSort(SortRequest<TField>? request, HashSet<TField> seen)
    {
        if (request is null)
            return Fail<Sort<TField>>(ValidationError.SortsMalformed());

        if (model.FindClientField(request.Field) is not { } field)
            return Fail<Sort<TField>>(ValidationError.UnknownField(request.Field));

        if (!field.Client.IsSortable)
            return Fail<Sort<TField>>(ValidationError.FieldNotSortable(request.Field));

        if (!seen.Add(request.Field))
            return Fail<Sort<TField>>(ValidationError.DuplicateSort(request.Field));

        return QueryResult<Sort<TField>>.Success(new Sort<TField>(request.Field, request.Direction));
    }

    /// <summary>
    /// Anything below 1 means "use the default", and anything above the maximum is lowered to it.
    /// </summary>
    public int ResolvePageSize(int? requested)
        => requested is > 0 and int size ? Math.Min(size, model.MaxPageSize) : model.DefaultPageSize;

    public ValidationError[] CheckSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return [];

        if (model.SearchFields.Count == 0)
            return [ValidationError.SearchNotSupported()];

        return search.Length > Options.MaxSearchLength ? [ValidationError.SearchTooLong(Options.MaxSearchLength)] : [];
    }

    public ValidationError[] CheckSpatialBounds(BoundingBox? bounds) => bounds switch
    {
        null => [],
        { IsValid: false } => [ValidationError.SpatialBoundsInvalid()],

        // Fail loudly rather than quietly return results that ignore the box.
        _ when model.SpatialFilter is null => [ValidationError.SpatialNotSupported()],

        _ => [],
    };

    private static QueryResult<T> Fail<T>(ValidationError error) => QueryResult<T>.Failure(error);

    private static QueryResult<IReadOnlyList<T>> Collect<T>(IEnumerable<QueryResult<T>> results)
    {
        QueryResult<T>[] all = [.. results];
        ValidationError[] errors = [.. all.SelectMany(result => result.Errors)];

        return errors.Length > 0
            ? QueryResult<IReadOnlyList<T>>.Failure(errors)
            : QueryResult<IReadOnlyList<T>>.Success([.. all.Select(result => result.Value)]);
    }
}
