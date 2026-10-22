using System.Collections;

namespace Qorel;

/// <summary>
/// A list of filters with a stable <see cref="CacheKey"/>. The same filters give the same key in any order, so a set
/// works as a cache key or a dictionary key.
/// </summary>
/// <remarks>
/// <see cref="QueryModel{TEntity,TField}.ParseFilters(IEnumerable{FilterRequest{TField}})"/> returns one for client
/// input. Use <see cref="Create"/> for filters built in code.
/// </remarks>
/// <typeparam name="TField">The enum that names the fields.</typeparam>
public sealed class FilterSet<TField> : IReadOnlyList<Filter<TField>>, IEquatable<FilterSet<TField>>
    where TField : struct, Enum
{
    private readonly Filter<TField>[] _filters;

    private FilterSet(Filter<TField>[] filters)
    {
        _filters = filters;
        CacheKey = string.Join(';', filters.Select(filter => filter.CacheKey));
    }

    /// <summary>
    /// A set with no filters. Its <see cref="CacheKey"/> is empty.
    /// </summary>
    public static FilterSet<TField> Empty { get; } = new([]);

    /// <summary>
    /// The <see cref="Filter{TField}.CacheKey"/> of every filter, sorted and joined with <c>;</c>.
    /// </summary>
    /// <example><c>Category.Equals="Food";Price.LessThan=5000</c></example>
    public string CacheKey { get; }

    /// <inheritdoc/>
    public int Count => _filters.Length;

    /// <inheritdoc/>
    public Filter<TField> this[int index] => _filters[index];

    /// <summary>
    /// Creates a set from <paramref name="filters"/>. Duplicates are dropped, and the filters are sorted by key, which
    /// also means the same set always produces the same SQL.
    /// </summary>
    public static FilterSet<TField> Create(IEnumerable<Filter<TField>> filters)
    {
        ArgumentNullException.ThrowIfNull(filters);

        Filter<TField>[] distinct = [.. filters.DistinctBy(filter => filter.CacheKey).OrderBy(filter => filter.CacheKey, StringComparer.Ordinal)];
        return distinct.Length == 0 ? Empty : new FilterSet<TField>(distinct);
    }

    /// <inheritdoc/>
    public IEnumerator<Filter<TField>> GetEnumerator() => ((IEnumerable<Filter<TField>>)_filters).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Returns true when both sets have the same <see cref="CacheKey"/>.
    /// </summary>
    public bool Equals(FilterSet<TField>? other)
        => other is not null && string.Equals(CacheKey, other.CacheKey, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as FilterSet<TField>);

    /// <inheritdoc/>
    public override int GetHashCode() => CacheKey.GetHashCode(StringComparison.Ordinal);

    /// <summary>
    /// Returns <see cref="CacheKey"/>.
    /// </summary>
    public override string ToString() => CacheKey;

    /// <summary>
    /// Returns true when both sets have the same <see cref="CacheKey"/>.
    /// </summary>
    public static bool operator ==(FilterSet<TField>? left, FilterSet<TField>? right) => Equals(left, right);

    /// <summary>
    /// Returns true when the sets have different keys.
    /// </summary>
    public static bool operator !=(FilterSet<TField>? left, FilterSet<TField>? right) => !Equals(left, right);
}
