using System.Reflection;

namespace Qorel;

/// <summary>
/// One item of a page as it comes back from the database: the projected item, and its values in each sort key, which
/// become the next cursor.
/// </summary>
internal sealed record PageRow<TResult>(TResult Item, object?[] SortValues)
{
    public static ConstructorInfo Constructor { get; } = typeof(PageRow<TResult>).GetConstructor([typeof(TResult), typeof(object?[])])!;
}
