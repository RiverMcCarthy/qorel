using System.Collections.Frozen;
using System.Linq.Expressions;

namespace Qorel;

/// <summary>
/// Declares a model's fields. Passed to <see cref="QueryModel{TEntity,TField}.Configure"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
public sealed class QueryModelBuilder<TEntity, TField>
    where TEntity : class
    where TField : struct, Enum
{
    private readonly string _modelName;
    private readonly List<(TField Field, Func<MappedField<TEntity, TField>> Build)> _fields = [];
    private readonly List<Sort<TField>> _defaultSorts = [];
    private TField? _id;
    private int? _defaultPageSize;
    private int? _maxPageSize;
    private Func<BoundingBox, Expression<Func<TEntity, bool>>>? _spatialFilter;

    internal QueryModelBuilder(string modelName) => _modelName = modelName;

    /// <summary>
    /// Maps the entity's unique id. Every model needs one: it is used to find items by id, and as the last sort so items
    /// with the same sort values still have a fixed order between pages.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the id is already mapped, or the field is mapped twice.</exception>
    public FieldBuilder<TEntity, TField, TValue> Id<TValue>(Field<TField, TValue> field, Expression<Func<TEntity, TValue>> selector)
        where TValue : IComparable<TValue>
    {
        if (_id is not null)
            throw new InvalidOperationException($"{_modelName} maps its id more than once.");

        FieldBuilder<TEntity, TField, TValue> builder = Map(field, selector);
        _id = field.Key;
        return builder;
    }

    /// <summary>
    /// Maps a field to the entity property or expression it reads. Chain <c>Filterable()</c> and <c>Sortable()</c> to
    /// let clients use it. Without them, the field can only be used in server-side filters and code.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the field is already mapped.</exception>
    public FieldBuilder<TEntity, TField, TValue> Map<TValue>(Field<TField, TValue> field, Expression<Func<TEntity, TValue>> selector)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(selector);

        if (_fields.Exists(mapped => EqualityComparer<TField>.Default.Equals(mapped.Field, field.Key)))
            throw new InvalidOperationException($"{_modelName} maps '{field.Key}' more than once.");

        var builder = new FieldBuilder<TEntity, TField, TValue>(field.Key, selector);
        _fields.Add((field.Key, builder.Build));
        return builder;
    }

    /// <summary>
    /// Sorts by <paramref name="field"/> when a query does not ask for a sort. Call it again to add a second sort for
    /// items the first considers equal. Without a default sort, queries are sorted by id.
    /// </summary>
    /// <remarks>The field does not have to be <c>Sortable()</c>, since clients do not choose it.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when the field is already a default sort.</exception>
    public QueryModelBuilder<TEntity, TField> DefaultSort<TValue>(Field<TField, TValue> field, SortDirection direction = SortDirection.Asc)
        where TValue : IComparable<TValue>
        => AddDefaultSort(field?.Key, direction);

    /// <inheritdoc cref="DefaultSort{TValue}(Field{TField, TValue}, SortDirection)"/>
    public QueryModelBuilder<TEntity, TField> DefaultSort<TValue>(Field<TField, TValue?> field, SortDirection direction = SortDirection.Asc)
        where TValue : struct, IComparable<TValue>
        => AddDefaultSort(field?.Key, direction);

    /// <summary>
    /// Sets the page sizes for this model, in place of <see cref="QorelOptions.DefaultPageSize"/> and
    /// <see cref="QorelOptions.MaxPageSize"/>. Leave either out to keep the option's value.
    /// </summary>
    /// <example>
    /// <code>
    /// model.PageSize(defaultSize: 200, maxSize: 1000); // e.g. points for a map
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">Thrown when neither size is given.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a size is less than 1.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the page sizes are already set.</exception>
    public QueryModelBuilder<TEntity, TField> PageSize(int? defaultSize = null, int? maxSize = null)
    {
        if (defaultSize is null && maxSize is null)
            throw new ArgumentException("Give a default size, a maximum size, or both.", nameof(defaultSize));

        if (defaultSize is not null)
            ArgumentOutOfRangeException.ThrowIfLessThan(defaultSize.Value, 1, nameof(defaultSize));

        if (maxSize is not null)
            ArgumentOutOfRangeException.ThrowIfLessThan(maxSize.Value, 1, nameof(maxSize));

        if (_defaultPageSize is not null || _maxPageSize is not null)
            throw new InvalidOperationException($"{_modelName} sets its page sizes more than once.");

        _defaultPageSize = defaultSize;
        _maxPageSize = maxSize;
        return this;
    }

    /// <summary>
    /// Lets clients filter by a bounding box. <paramref name="predicate"/> returns the condition for one box, which is
    /// added to the query alongside the other filters.
    /// </summary>
    /// <example>
    /// <code>
    /// model.SpatialFilter(box => e => e.Latitude &gt;= box.MinLatitude &amp;&amp; e.Latitude &lt;= box.MaxLatitude
    ///     &amp;&amp; e.Longitude &gt;= box.MinLongitude &amp;&amp; e.Longitude &lt;= box.MaxLongitude);
    /// </code>
    /// </example>
    /// <exception cref="InvalidOperationException">Thrown when a spatial filter is already set.</exception>
    public QueryModelBuilder<TEntity, TField> SpatialFilter(Func<BoundingBox, Expression<Func<TEntity, bool>>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        if (_spatialFilter is not null)
            throw new InvalidOperationException($"{_modelName} sets a spatial filter more than once.");

        _spatialFilter = predicate;
        return this;
    }

    internal BuiltModel<TEntity, TField> Build(QorelOptions options)
    {
        if (_id is not { } id)
            throw new InvalidOperationException($"{_modelName} has no id. Map one with model.Id(...) in Configure.");

        var fields = _fields
            .Select(field => field.Build())
            .ToFrozenDictionary(field => field.Field);

        foreach (Sort<TField> sort in _defaultSorts.Where(sort => !fields.ContainsKey(sort.Field)))
            throw new InvalidOperationException($"{_modelName} sorts by '{sort.Field}' by default, but does not map it.");

        int defaultPageSize = _defaultPageSize ?? options.DefaultPageSize;
        int maxPageSize = _maxPageSize ?? options.MaxPageSize;

        if (defaultPageSize > maxPageSize)
        {
            throw new InvalidOperationException(
                $"{_modelName} has a default page size of {defaultPageSize}, more than its maximum of {maxPageSize}. Set both with PageSize(...).");
        }

        // Declaration order, so ClientFields lists fields the way the model declares them.
        MappedField<TEntity, TField>[] inOrder = [.. _fields.Select(field => fields[field.Field])];

        return new BuiltModel<TEntity, TField>
        {
            Name = _modelName,
            Options = options,
            Fields = fields,
            Id = fields[id],
            DefaultSorts = [.. _defaultSorts],
            DefaultPageSize = defaultPageSize,
            MaxPageSize = maxPageSize,
            SearchFields = [.. inOrder.Where(field => field.Client.IsSearchable)],
            ClientFields = [.. inOrder.Select(field => field.Client).Where(client => client.IsVisible)],
            SpatialFilter = _spatialFilter,
        };
    }

    private QueryModelBuilder<TEntity, TField> AddDefaultSort(TField? field, SortDirection direction)
    {
        if (field is not { } key)
            throw new ArgumentNullException(nameof(field));

        if (_defaultSorts.Exists(sort => EqualityComparer<TField>.Default.Equals(sort.Field, key)))
            throw new InvalidOperationException($"{_modelName} sorts by '{key}' by default more than once.");

        _defaultSorts.Add(new Sort<TField>(key, direction));
        return this;
    }
}
