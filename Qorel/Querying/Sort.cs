namespace Qorel.Querying;

internal interface ISort<TKey> where TKey : Enum
{
    TKey FieldKey { get; }
    SortDirection Direction { get; }
}

internal sealed record Sort<TKey>(TKey FieldKey, SortDirection Direction) : ISort<TKey>
    where TKey : Enum;
