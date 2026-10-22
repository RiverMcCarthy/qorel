using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Qorel;

/// <summary>
/// One field of a <see cref="QueryModel{TEntity,TField}"/>: how to read it from the entity, and what clients may do with it.
/// </summary>
/// <remarks>
/// The model keeps its fields in one dictionary, so they share this base class. The subclass knows the value type,
/// which is where everything typed happens: reading filter values, building predicates, sorting, and cursor values.
/// </remarks>
internal abstract class MappedField<TEntity, TField>(ClientField<TField> client)
    where TEntity : class
    where TField : struct, Enum
{
    public TField Field => Client.Field;

    public Type ValueType => Client.ValueType;

    /// <summary>
    /// What clients may do with the field.
    /// </summary>
    public ClientField<TField> Client { get; } = client;

    /// <summary>
    /// True if the value can be null, which changes how the field sorts. See <see cref="OrderBy"/>.
    /// </summary>
    public abstract bool CanBeNull { get; }

    /// <summary>
    /// Builds a filter from a client's JSON value, or returns null when the value is not valid for the operator.
    /// </summary>
    public abstract Filter<TField>? TryCreateFilter(FilterOperator op, JsonElement value);

    /// <summary>
    /// Builds an <c>Equals</c> filter for <paramref name="value"/>, as used to find an item by id.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not the field's type.</exception>
    public abstract Filter<TField> CreateEqualsFilter<TId>(TId value);

    /// <summary>
    /// The filter as an expression tree, for a LINQ provider to translate.
    /// </summary>
    public abstract Expression<Func<TEntity, bool>> ToExpression(Filter<TField> filter);

    /// <summary>
    /// The filter as a delegate, for data in memory.
    /// </summary>
    public abstract Func<TEntity, bool> ToDelegate(Filter<TField> filter);

    /// <summary>
    /// Reads the field from <paramref name="entity"/>, for building multi-field expressions with one parameter.
    /// </summary>
    public abstract Expression Read(ParameterExpression entity);

    /// <summary>
    /// <paramref name="value"/> as a typed query parameter.
    /// </summary>
    public abstract Expression Parameter(object? value);

    /// <summary>
    /// Sorts by this field. Nulls sort last ascending and first descending, on every database.
    /// </summary>
    public abstract IOrderedQueryable<TEntity> OrderBy(IQueryable<TEntity> source, bool descending);

    /// <summary>
    /// Sorts by this field after the existing sorts. Nulls are placed as in <see cref="OrderBy"/>.
    /// </summary>
    public abstract IOrderedQueryable<TEntity> ThenBy(IOrderedQueryable<TEntity> source, bool descending);

    public abstract JsonElement WriteCursorValue(object? value);

    public abstract bool TryReadCursorValue(JsonElement json, out object? value);
}

/// <inheritdoc/>
internal sealed class MappedField<TEntity, TField, TValue> : MappedField<TEntity, TField>
    where TEntity : class
    where TField : struct, Enum
{
    private readonly Expression<Func<TEntity, TValue>> _selector;

    // Sorts nulls after values. Only built for fields that can be null.
    private readonly Expression<Func<TEntity, bool>>? _isNull;
    private readonly bool _ignoreCase;

    // Compiled the first time the field is filtered in memory, then reused.
    private readonly Lazy<Func<TEntity, TValue>> _getValue;

    public MappedField(ClientField<TField> client, Expression<Func<TEntity, TValue>> selector, bool ignoreCase)
        : base(client)
    {
        _selector = selector;
        _ignoreCase = ignoreCase;
        _getValue = new(selector.Compile);
        CanBeNull = IsNullable(selector);

        if (CanBeNull)
            _isNull = Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(selector.Body, QueryExpressions.Null(typeof(TValue))), selector.Parameters);
    }

    public override bool CanBeNull { get; }

    public override Filter<TField>? TryCreateFilter(FilterOperator op, JsonElement value) => op.GetOperand() switch
    {
        Operand.None => FieldFilter<TField, TValue>.CreateNullCheck(Field, op),
        Operand.List => FieldValueJson.TryReadArray(value, out List<TValue> values) ? FieldFilter<TField, TValue>.CreateList(Field, op, values) : null,
        _ => FieldValueJson.TryRead(value, out TValue single) ? FieldFilter<TField, TValue>.Create(Field, op, single) : null,
    };

    public override Filter<TField> CreateEqualsFilter<TId>(TId value) => value is TValue typed
        ? FieldFilter<TField, TValue>.Create(Field, FilterOperator.Equals, typed)
        : throw new ArgumentException($"'{Field}' is a {typeof(TValue).Name} field, but the value is {typeof(TId).Name}.", nameof(value));

    public override Expression<Func<TEntity, bool>> ToExpression(Filter<TField> filter)
        => FilterExpressions.Build(_selector, Typed(filter), _ignoreCase);

    public override Func<TEntity, bool> ToDelegate(Filter<TField> filter)
        => InMemoryFilters.Build(_getValue.Value, Typed(filter), _ignoreCase);

    public override Expression Read(ParameterExpression entity)
        => ParameterReplacer.Replace(_selector, entity);

    public override Expression Parameter(object? value) => QueryExpressions.Parameter((TValue)value!);

    // Sorting on "is null" first puts nulls in the same place on every database. Ascending, false sorts before true,
    // so non-null values come first and nulls last. Descending is the exact reverse.
    public override IOrderedQueryable<TEntity> OrderBy(IQueryable<TEntity> source, bool descending)
    {
        if (_isNull is null)
            return descending ? source.OrderByDescending(_selector) : source.OrderBy(_selector);

        IOrderedQueryable<TEntity> byNull = descending ? source.OrderByDescending(_isNull) : source.OrderBy(_isNull);
        return descending ? byNull.ThenByDescending(_selector) : byNull.ThenBy(_selector);
    }

    public override IOrderedQueryable<TEntity> ThenBy(IOrderedQueryable<TEntity> source, bool descending)
    {
        if (_isNull is not null)
            source = descending ? source.ThenByDescending(_isNull) : source.ThenBy(_isNull);

        return descending ? source.ThenByDescending(_selector) : source.ThenBy(_selector);
    }

    public override JsonElement WriteCursorValue(object? value) => FieldValueJson.Write((TValue)value!);

    public override bool TryReadCursorValue(JsonElement json, out object? value)
    {
        bool read = FieldValueJson.TryRead(json, out TValue typed);
        value = typed;
        return read;
    }

    private FieldFilter<TField, TValue> Typed(Filter<TField> filter) => filter as FieldFilter<TField, TValue>
        ?? throw new ArgumentException(
            $"The filter on '{Field}' compares {filter.ValueType.Name} values, but the field is mapped as {typeof(TValue).Name}.",
            nameof(filter));

    // Value types are nullable only as Nullable<T>. A reference type is trusted to be non-null when the selector reads a
    // property or field whose nullable annotation says so, e.g. "string Name" rather than "string? Name".
    private static bool IsNullable(Expression<Func<TEntity, TValue>> selector)
    {
        if (typeof(TValue).IsValueType)
            return Nullable.GetUnderlyingType(typeof(TValue)) is not null;

        NullabilityInfoContext context = new();
        NullabilityInfo? nullability = (selector.Body as MemberExpression)?.Member switch
        {
            PropertyInfo property => context.Create(property),
            FieldInfo field => context.Create(field),
            _ => null,
        };

        return nullability?.ReadState != NullabilityState.NotNull;
    }
}
