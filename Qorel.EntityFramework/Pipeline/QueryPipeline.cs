using System.Linq.Expressions;
using System.Text.Json;

using Qorel.EntityFramework.Cursors;
using Qorel.EntityFramework.Mapping;
using Qorel.Exceptions;
using Qorel.Filters;
using Qorel.Querying;
using Qorel.Results;
using Qorel.Schema;

using Microsoft.EntityFrameworkCore;

namespace Qorel.EntityFramework.Pipeline;

/// <summary>
/// Default implementation of <see cref="IQueryPipeline{TEntity,TKey}"/> backed by EF Core.
/// </summary>
/// <remarks>
/// Register as a scoped service via <c>AddQorelPipeline()</c>.
/// </remarks>
/// <typeparam name="TEntity">The EF Core entity type. Must be a reference type.</typeparam>
/// <typeparam name="TKey">Enum type identifying filterable/sortable fields.</typeparam>
public sealed class QueryPipeline<TEntity, TKey> : IQueryPipeline<TEntity, TKey>
    where TEntity : class
    where TKey : Enum
{
    private readonly FieldMap<TEntity, TKey> _fieldMap;
    private readonly DbContext _context;
    private readonly QueryRequestConverter<TKey> _converter;

    /// <summary>
    /// Initialises the pipeline. Register via <c>AddQorelPipeline()</c> and inject <see cref="IQueryPipeline{TEntity,TKey}"/>.
    /// </summary>
    internal QueryPipeline(
        FieldMap<TEntity, TKey> fieldMap,
        FieldSchema<TKey> schema,
        PaginationOptions pagination,
        DbContext context)
    {
        _fieldMap = fieldMap;
        _context = context;

        // Fail at startup if misconfigured.
        schema.EnsureInitialised();

        if (!_fieldMap.Has(fieldMap.IdKey))
        {
            throw new InvalidOperationException(
                $"{fieldMap.GetType().Name}.IdKey ('{fieldMap.IdKey}') is not registered in the field map. " +
                $"Add it via map.Add(...) in Build().");
        }

        if (!_fieldMap.Has(fieldMap.TiebreakerKey))
        {
            throw new InvalidOperationException(
                $"{fieldMap.GetType().Name}.TiebreakerKey ('{fieldMap.TiebreakerKey}') is not registered in the field map. " +
                $"Add it via map.Add(...) in Build().");
        }

        _converter = new QueryRequestConverter<TKey>(pagination, schema);
    }

    private IQueryable<TEntity> BaseQuery => _context.Set<TEntity>().AsNoTracking();

    /// <inheritdoc/>
    public async Task<QueryResult<Page<TResult>>> QueryAsync<TResult>(
        QueryRequest<TKey> request,
        Expression<Func<TEntity, TResult>> project,
        IReadOnlyList<IFilter<TKey>>? serverFilters = null,
        CancellationToken ct = default)
    {
        QueryResult<ResolvedQuery<TKey>> conversion = _converter.Convert(request);

        if (conversion.IsFailure)
            return QueryResult<Page<TResult>>.Failure(conversion.Errors);

        try
        {
            ResolvedQuery<TKey> options = AppendServerFilters(conversion.Value, serverFilters);
            return QueryResult<Page<TResult>>.Success(await ExecuteAsync(options, project, ct));
        }
        catch (InvalidCursorException)
        {
            return QueryResult<Page<TResult>>.Failure(ValidationError.CursorInvalid());
        }
    }

    /// <inheritdoc/>
    public async Task<QueryResult<Page<TResult>>> QueryAsync<TResult>(
        QueryBuilder<TKey> query,
        Expression<Func<TEntity, TResult>> project,
        IReadOnlyList<IFilter<TKey>>? serverFilters = null,
        CancellationToken ct = default)
    {
        try
        {
            ResolvedQuery<TKey> options = AppendServerFilters(query.Build(), serverFilters);
            return QueryResult<Page<TResult>>.Success(await ExecuteAsync(options, project, ct));
        }
        catch (InvalidCursorException)
        {
            return QueryResult<Page<TResult>>.Failure(ValidationError.CursorInvalid());
        }
    }

    /// <inheritdoc/>
    public async Task<TResult?> FindAsync<TId, TResult>(
        TId id,
        Expression<Func<TEntity, TResult>> project,
        CancellationToken ct = default)
    {
        IFieldAccessor<TEntity> idAccessor = _fieldMap.Get(_fieldMap.IdKey);
        Expression<Func<TEntity, bool>> predicate = idAccessor.BuildEqualityPredicate(id!);

        return await BaseQuery
            .Where(predicate)
            .Select(project)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        IReadOnlyList<IFilter<TKey>> filters,
        Expression<Func<TEntity, TResult>> project,
        CancellationToken ct = default)
    {
        return await _fieldMap.ApplyFilters(BaseQuery, filters)
            .Select(project)
            .ToListAsync(ct);
    }

    private async Task<Page<TResult>> ExecuteAsync<TResult>(
        ResolvedQuery<TKey> options,
        Expression<Func<TEntity, TResult>> project,
        CancellationToken ct)
    {
        IQueryable<TEntity> query = BuildQuery(options, out IReadOnlyList<ISort<TKey>> sorts);

        var sortAccessors = sorts.Select(s => _fieldMap.Get(s.FieldKey)).ToList();

        List<CursorProjection.CursorRow<TResult>> rows = await query
            .Select(CursorProjection.Wrap(project, sortAccessors))
            .Take(options.PageSize + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > options.PageSize;
        var nextCursor = hasMore
            ? BuildCursor(rows[options.PageSize - 1].RawSortValues, sortAccessors)
            : null;

        return new Page<TResult>
        {
            Items = [.. rows.Take(options.PageSize).Select(r => r.Item)],
            NextCursor = nextCursor,
        };
    }

    private IQueryable<TEntity> BuildQuery(ResolvedQuery<TKey> options, out IReadOnlyList<ISort<TKey>> sorts)
    {
        IQueryable<TEntity> query = _fieldMap.ApplyFilters(BaseQuery, options.Filters);

        if (options.SpatialBounds is { } bounds && _fieldMap is ISpatialFilter<TEntity> spatial)
            query = query.Where(spatial.BuildSpatialPredicate(bounds));

        sorts = NormalizeSorts(options.Sorts);
        query = ApplySorts(query, sorts);

        if (!string.IsNullOrEmpty(options.Cursor))
        {
            CursorToken token = CursorEncoder.Decode(options.Cursor);
            query = query.Where(CursorPredicate.Build(token, sorts, _fieldMap));
        }

        return query;
    }

    private IQueryable<TEntity> ApplySorts(IQueryable<TEntity> query, IReadOnlyList<ISort<TKey>> sorts)
    {
        IOrderedQueryable<TEntity>? ordered = null;

        foreach (ISort<TKey> sort in sorts)
        {
            IFieldAccessor<TEntity> accessor = _fieldMap.Get(sort.FieldKey);
            var desc = sort.Direction == SortDirection.Desc;
            ordered = ordered is null
                ? accessor.OrderBy(query, desc)
                : accessor.ThenBy(ordered, desc);
        }

        return ordered ?? query;
    }

    private List<ISort<TKey>> NormalizeSorts(IReadOnlyList<ISort<TKey>> sorts)
    {
        var list = sorts.ToList();

        // Add tiebreaker as the final sort to guarantee deterministic cursor pagination.
        if (!list.Any(s => EqualityComparer<TKey>.Default.Equals(s.FieldKey, _fieldMap.TiebreakerKey)))
            list.Add(new Sort<TKey>(_fieldMap.TiebreakerKey, SortDirection.Asc));

        return list;
    }

    private static ResolvedQuery<TKey> AppendServerFilters(ResolvedQuery<TKey> options, IReadOnlyList<IFilter<TKey>>? serverFilters)
    {
        if (serverFilters is not { Count: > 0 })
            return options;

        return options with { Filters = [.. options.Filters, .. serverFilters] };
    }

    private static string BuildCursor(object?[] rawSortValues, IReadOnlyList<IFieldAccessor<TEntity>> sortAccessors)
    {
        var values = new string[sortAccessors.Count];
        for (var i = 0; i < sortAccessors.Count; i++)
            values[i] = JsonSerializer.Serialize(rawSortValues[i], sortAccessors[i].Type);

        return CursorEncoder.Encode(new CursorToken(values));
    }
}
