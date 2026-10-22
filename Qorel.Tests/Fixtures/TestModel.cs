namespace Qorel.Tests.Fixtures;

public enum TestField { Id, Name, Price, Stock, Category, CreatedAt, Score, OwnerId, Nickname }

public enum TestCategory { Alpha, Beta, Gamma }

public sealed record TestEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "";
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public TestCategory Category { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public double? Score { get; init; }
    public Guid OwnerId { get; init; }
    public string? Nickname { get; init; }
}

public static class TestFields
{
    public static readonly Field<TestField, Guid> Id = new(TestField.Id);
    public static readonly TextField<TestField> Name = new(TestField.Name);
    public static readonly ComparableField<TestField, decimal> Price = new(TestField.Price);
    public static readonly ComparableField<TestField, int> Stock = new(TestField.Stock);
    public static readonly Field<TestField, TestCategory> Category = new(TestField.Category);
    public static readonly ComparableField<TestField, DateTimeOffset> CreatedAt = new(TestField.CreatedAt);
    public static readonly Field<TestField, double?> Score = new(TestField.Score);
    public static readonly Field<TestField, Guid> OwnerId = new(TestField.OwnerId);
    public static readonly TextField<TestField> Nickname = new(TestField.Nickname);
}

/// <summary>
/// Covers each kind of field: sortable and filterable, filterable only, sortable only (<c>Stock</c>), and server-side
/// only (<c>OwnerId</c>).
/// </summary>
public sealed class TestQueryModel : QueryModel<TestEntity, TestField>
{
    protected override void Configure(QueryModelBuilder<TestEntity, TestField> model)
    {
        model.Id(TestFields.Id, e => e.Id);
        model.Map(TestFields.Name, e => e.Name).Filterable().Sortable();
        model.Map(TestFields.Price, e => e.Price).Filterable().Sortable();
        model.Map(TestFields.Stock, e => e.Stock).Sortable();
        model.Map(TestFields.Category, e => e.Category).Filterable();
        model.Map(TestFields.CreatedAt, e => e.CreatedAt).Filterable().Sortable();
        model.Map(TestFields.Score, e => e.Score).Filterable();
        model.Map(TestFields.OwnerId, e => e.OwnerId);
        model.Map(TestFields.Nickname, e => e.Nickname!).Filterable().Sortable().IgnoreCase();
    }
}
