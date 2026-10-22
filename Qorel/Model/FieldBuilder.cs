using System.Collections.Frozen;
using System.Linq.Expressions;

namespace Qorel;

/// <summary>
/// Sets what clients may do with one mapped field. Returned by <see cref="QueryModelBuilder{TEntity,TField}.Map{TValue}"/>.
/// A field with no options is only usable in server-side filters and code.
/// </summary>
/// <remarks>
/// <c>Sortable()</c>, <c>Searchable()</c> and <c>IgnoreCase()</c> are extension methods in
/// <see cref="FieldBuilderExtensions"/>, so they are only offered for value types that support them.
/// </remarks>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
/// <typeparam name="TValue">The field's value type.</typeparam>
public sealed class FieldBuilder<TEntity, TField, TValue>
    where TEntity : class
    where TField : struct, Enum
{
    private readonly TField _field;
    private readonly Expression<Func<TEntity, TValue>> _selector;
    private IReadOnlySet<FilterOperator> _operators = FrozenSet<FilterOperator>.Empty;
    private bool _sortable;
    private bool _searchable;
    private bool _ignoreCase;

    internal FieldBuilder(TField field, Expression<Func<TEntity, TValue>> selector)
    {
        _field = field;
        _selector = selector;
    }

    /// <summary>
    /// Lets clients filter the field with every operator its type supports.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown when <typeparamref name="TValue"/> cannot be filtered.</exception>
    public FieldBuilder<TEntity, TField, TValue> Filterable()
    {
        _operators = FilterOperatorRules.SupportedBy(typeof(TValue));
        return this;
    }

    /// <summary>
    /// Lets clients filter the field with only <paramref name="operators"/>, e.g. <see cref="FilterOperators.Comparison"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="operators"/> is empty, or has an operator <typeparamref name="TValue"/> does not support.
    /// </exception>
    public FieldBuilder<TEntity, TField, TValue> Filterable(IEnumerable<FilterOperator> operators)
    {
        ArgumentNullException.ThrowIfNull(operators);

        var requested = operators.ToFrozenSet();
        FrozenSet<FilterOperator> supported = FilterOperatorRules.SupportedBy(typeof(TValue));

        if (requested.Count == 0)
            throw new ArgumentException($"'{_field}' needs at least one operator. Leave Filterable() out to stop clients filtering it.", nameof(operators));

        FilterOperator[] unsupported = [.. requested.Where(op => !supported.Contains(op))];
        if (unsupported.Length > 0)
        {
            throw new ArgumentException(
                $"'{_field}' is a {typeof(TValue).Name} field, which does not support {string.Join(", ", unsupported)}. " +
                $"It supports {string.Join(", ", supported.Order())}.",
                nameof(operators));
        }

        _operators = requested;
        return this;
    }

    internal void SetSortable() => _sortable = true;

    internal void SetSearchable() => _searchable = true;

    internal void SetIgnoreCase() => _ignoreCase = true;

    internal MappedField<TEntity, TField> Build()
    {
        var client = new ClientField<TField>(_field, typeof(TValue), _operators, _sortable, _searchable);
        return new MappedField<TEntity, TField, TValue>(client, _selector, _ignoreCase);
    }
}
