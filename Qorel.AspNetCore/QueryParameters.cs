using System.Globalization;

using Microsoft.AspNetCore.Mvc;

namespace Qorel;

/// <summary>
/// A query sent as query string parameters, for GET endpoints, which can be cached and bookmarked:
/// <code>
/// GET /products?filters=[{"field":"Category","operator":"Equals","value":"Books"}]&amp;sort=price:desc,name&amp;search=red&amp;pageSize=20&amp;cursor=…
/// </code>
/// </summary>
/// <remarks>
/// In a minimal API, bind it with <c>[AsParameters]</c>, which also documents each parameter in OpenAPI. In a controller,
/// use <c>[FromQuery]</c>. Then pass it to
/// <see cref="QueryPipelineExtensions.QueryAsync{TEntity, TField, TResult}(IQueryPipeline{TEntity, TField}, QueryParameters{TField}, System.Linq.Expressions.Expression{Func{TEntity, TResult}}, IEnumerable{Filter{TField}}?, CancellationToken)"/>.
/// </remarks>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
public sealed class QueryParameters<TField>
    where TField : struct, Enum
{
    /// <summary>
    /// A JSON array of filters, in the same format as <see cref="QueryRequest{TField}.Filters"/>. URL-encode it.
    /// </summary>
    [FromQuery(Name = "filters")]
    public string? Filters { get; init; }

    /// <summary>
    /// Comma-separated sorts, each a field name with an optional direction, e.g. <c>price:desc,name</c>.
    /// </summary>
    [FromQuery(Name = "sort")]
    public string? Sort { get; init; }

    /// <summary>
    /// Text to search for. See <see cref="QueryRequest{TField}.Search"/>.
    /// </summary>
    [FromQuery(Name = "search")]
    public string? Search { get; init; }

    /// <summary>
    /// How many items to return. See <see cref="QueryRequest{TField}.PageSize"/>.
    /// </summary>
    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; init; }

    /// <summary>
    /// The <see cref="Page{T}.NextCursor"/> from the previous page.
    /// </summary>
    [FromQuery(Name = "cursor")]
    public string? Cursor { get; init; }

    /// <summary>
    /// A bounding box as <c>minLatitude,minLongitude,maxLatitude,maxLongitude</c>, e.g. <c>51.4,-0.2,51.6,0.1</c>.
    /// </summary>
    [FromQuery(Name = "bounds")]
    public string? Bounds { get; init; }

    /// <summary>
    /// Reads the parameters into a <see cref="QueryRequest{TField}"/>. Fails when one cannot be read, e.g. filters that
    /// are not valid JSON or a sort on a field that does not exist. The request still needs checking against the model,
    /// which the pipeline does.
    /// </summary>
    public QueryResult<QueryRequest<TField>> ToRequest()
    {
        QueryResult<IReadOnlyList<FilterRequest<TField>?>> filters = RequestJson.ReadFilters<TField>(Filters);
        QueryResult<IReadOnlyList<SortRequest<TField>>> sorts = ReadSorts(Sort);
        QueryResult<BoundingBox?> bounds = ReadBounds(Bounds);

        ValidationError[] errors = [.. filters.Errors, .. sorts.Errors, .. bounds.Errors];

        if (errors.Length > 0)
            return QueryResult<QueryRequest<TField>>.Failure(errors);

        return QueryResult<QueryRequest<TField>>.Success(new QueryRequest<TField>
        {
            // A null entry stays null here, the same as one in a request body, and the model reports it as malformed.
            Filters = (IReadOnlyList<FilterRequest<TField>>)filters.Value,
            Sorts = sorts.Value,
            Search = Search,
            PageSize = PageSize,
            Cursor = Cursor,
            SpatialBounds = bounds.Value,
        });
    }

    private static QueryResult<IReadOnlyList<SortRequest<TField>>> ReadSorts(string? text)
    {
        List<SortRequest<TField>> sorts = [];
        List<ValidationError> errors = [];

        foreach (string sort in (text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] parts = sort.Split(':', StringSplitOptions.TrimEntries);

            if (parts.Length > 2 || !TryReadDirection(parts.ElementAtOrDefault(1), out SortDirection direction))
                errors.Add(ValidationError.SortsMalformed());
            else if (!EnumNames<TField>.TryParse(parts[0], out TField field))
                errors.Add(ValidationError.UnknownField(parts[0]));
            else
                sorts.Add(new SortRequest<TField> { Field = field, Direction = direction });
        }

        return errors.Count > 0
            ? QueryResult<IReadOnlyList<SortRequest<TField>>>.Failure(errors)
            : QueryResult<IReadOnlyList<SortRequest<TField>>>.Success(sorts);
    }

    // No direction means ascending.
    private static bool TryReadDirection(string? text, out SortDirection direction)
    {
        direction = SortDirection.Asc;
        return text is null || EnumNames<SortDirection>.TryParse(text, out direction);
    }

    private static QueryResult<BoundingBox?> ReadBounds(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return QueryResult<BoundingBox?>.Success(null);

        double[] coordinates = [.. text.Split(',').Select(ParseCoordinate)];

        // A coordinate that is not a number reads as NaN, which the box's own range check rejects later.
        return coordinates.Length == 4
            ? QueryResult<BoundingBox?>.Success(new BoundingBox(coordinates[0], coordinates[1], coordinates[2], coordinates[3]))
            : QueryResult<BoundingBox?>.Failure(ValidationError.SpatialBoundsInvalid());
    }

    private static double ParseCoordinate(string text)
        => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : double.NaN;
}
