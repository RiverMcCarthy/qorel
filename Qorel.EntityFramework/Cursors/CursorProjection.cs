using System.Linq.Expressions;
using System.Reflection;

using Qorel.EntityFramework.Mapping;

namespace Qorel.EntityFramework.Cursors;

/// <summary>
/// Wraps a caller projection expression in a <see cref="CursorRow{T}"/> so that
/// the pipeline captures sort-field values for cursor building in a single SQL select.
/// </summary>
internal static class CursorProjection
{
    /// <summary>
    /// Holds the projected DTO and the raw sort-field values needed to build the next cursor.
    /// </summary>
    internal sealed record CursorRow<T>(T Item, object?[] RawSortValues);

    // ConstructorInfo resolved once per TResult at type initialisation.
    private static class RowCtor<TResult>
    {
        public static readonly ConstructorInfo Value =
            typeof(CursorRow<TResult>).GetConstructor([typeof(TResult), typeof(object[])])!;
    }

    /// <summary>
    /// Wraps the caller's projection in a <see cref="CursorRow{TResult}"/> expression,
    /// appending a sort-value array so cursor building requires no extra db round-trip.
    /// </summary>
    internal static Expression<Func<TEntity, CursorRow<TResult>>> Wrap<TEntity, TResult>(
        Expression<Func<TEntity, TResult>> projection,
        IReadOnlyList<IFieldAccessor<TEntity>> sortAccessors)
    {
        var param = projection.Parameters[0];

        var sortValues = Expression.NewArrayInit(
            typeof(object),
            sortAccessors.Select(a => Expression.Convert(a.Inline(param), typeof(object))));

        var body = Expression.New(RowCtor<TResult>.Value, projection.Body, sortValues);

        return Expression.Lambda<Func<TEntity, CursorRow<TResult>>>(body, param);
    }
}
