using Qorel.Filters;
using Qorel.Results;
using Qorel.Schema;

namespace Qorel.Querying;

internal sealed class QueryRequestConverter<TKey>(PaginationOptions pagination, FieldSchema<TKey> schema)
    where TKey : Enum
{
    public QueryResult<ResolvedQuery<TKey>> Convert(QueryRequest<TKey> request)
    {
        var errors = new List<ValidationError>();

        QueryResult<IReadOnlyList<IFilter<TKey>>> filterResult = schema.ParseFilters(request.Filters);
        if (filterResult.IsFailure)
            errors.AddRange(filterResult.Errors);

        var sorts = new List<Sort<TKey>>();
        foreach (SortRequest<TKey> descriptor in request.Sorts)
        {
            if (!schema.TryGet(descriptor.FieldKey, out FieldMetadata<TKey>? metadata))
                errors.Add(ValidationError.InvalidField(descriptor.FieldKey));
            else if (!metadata.IsSortable)
                errors.Add(ValidationError.FieldNotSortable(descriptor.FieldKey));
            else
                sorts.Add(new Sort<TKey>(descriptor.FieldKey, descriptor.Direction));
        }

        if (errors.Count > 0)
            return QueryResult<ResolvedQuery<TKey>>.Failure(errors);

        var pageSize = request.PageSize <= 0 ? pagination.Default : request.PageSize;

        return QueryResult<ResolvedQuery<TKey>>.Success(new()
        {
            Filters = filterResult.Value,
            Sorts = sorts.AsReadOnly(),
            Cursor = request.Cursor,
            PageSize = Math.Clamp(pageSize, 1, pagination.Maximum),
            SpatialBounds = request.SpatialBounds,
        });
    }
}
