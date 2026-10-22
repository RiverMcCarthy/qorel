using Microsoft.Extensions.DependencyInjection;

using Qorel.Tests.Fixtures;

namespace Qorel.Tests;

/// <summary>
/// How a model is declared, and the mistakes it catches when it is built.
/// </summary>
public class QueryModelTests
{
    // --- Client fields --------------------------------------------------------

    [Fact]
    public void ClientFields_LeavesOutServerOnlyFields()
        => new TestQueryModel().ClientFields.Select(f => f.Field).Should().NotContain([TestField.Id, TestField.OwnerId]);

    [Fact]
    public void ClientFields_KeepsDeclarationOrder()
        => new TestQueryModel().ClientFields.Select(f => f.Field).Should().Equal(
            TestField.Name, TestField.Price, TestField.Stock, TestField.Category, TestField.CreatedAt, TestField.Score, TestField.Nickname);

    [Fact]
    public void ClientFields_DescribeEachField()
    {
        ClientField<TestField> stock = new TestQueryModel().ClientFields.Single(f => f.Field == TestField.Stock);

        stock.ValueType.Should().Be<int>();
        stock.IsSortable.Should().BeTrue();
        stock.IsFilterable.Should().BeFalse();
        stock.AllowedOperators.Should().BeEmpty();
    }

    [Fact]
    public void ClientFields_IncludeSearchOnlyFields()
    {
        var model = new ConfiguredModel(m => m.Map(TestFields.Nickname, e => e.Nickname!).Searchable());

        ClientField<TestField> nickname = model.ClientFields.Single();
        nickname.IsSearchable.Should().BeTrue();
        nickname.IsFilterable.Should().BeFalse();
    }

    // --- Default operators per type -------------------------------------------

    public enum Colour { Red, Green }

    public static TheoryData<Type, FilterOperator[]> DefaultOperatorCases() => new()
    {
        { typeof(string), [.. FilterOperators.Equality, .. FilterOperators.Text, .. FilterOperators.NullCheck] },
        { typeof(bool), [.. FilterOperators.Equality] },
        { typeof(Guid), [.. FilterOperators.Equality] },
        { typeof(Colour), [.. FilterOperators.Equality] },
        { typeof(int), [.. FilterOperators.Equality, .. FilterOperators.Comparison] },
        { typeof(decimal), [.. FilterOperators.Equality, .. FilterOperators.Comparison] },
        { typeof(DateTimeOffset), [.. FilterOperators.Equality, .. FilterOperators.Comparison] },
        { typeof(DateOnly), [.. FilterOperators.Equality, .. FilterOperators.Comparison] },
        { typeof(TimeSpan), [.. FilterOperators.Equality, .. FilterOperators.Comparison] },
        { typeof(int?), [.. FilterOperators.Equality, .. FilterOperators.Comparison, .. FilterOperators.NullCheck] },
        { typeof(Colour?), [.. FilterOperators.Equality, .. FilterOperators.NullCheck] },
    };

    [Theory]
    [MemberData(nameof(DefaultOperatorCases))]
    public void Filterable_AllowsEveryOperatorTheTypeSupports(Type type, FilterOperator[] expected)
    {
        var model = (IOperatorProbe)Activator.CreateInstance(typeof(SingleFieldModel<>).MakeGenericType(type))!;

        model.Operators.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Filterable_WithOperators_NarrowsTheSet()
    {
        var model = new ConfiguredModel(m => m.Map(TestFields.Price, e => e.Price).Filterable(FilterOperators.Comparison));

        model.ClientFields.Single().AllowedOperators.Should().BeEquivalentTo(FilterOperators.Comparison);
    }

    // --- Mistakes caught when the model is built ------------------------------

    [Fact]
    public void Build_NoId_Throws()
        => Building(_ => { }, withId: false).Should().Throw<InvalidOperationException>().WithMessage("*has no id*");

    [Fact]
    public void Build_IdMappedTwice_Throws()
        => Building(m => m.Id(TestFields.OwnerId, e => e.OwnerId))
            .Should().Throw<InvalidOperationException>().WithMessage("*id more than once*");

    [Fact]
    public void Build_FieldMappedTwice_Throws()
        => Building(m =>
        {
            m.Map(TestFields.Name, e => e.Name);
            m.Map(TestFields.Name, e => e.Name);
        }).Should().Throw<InvalidOperationException>().WithMessage("*'Name' more than once*");

    [Fact]
    public void Filterable_OperatorTheTypeCannotSupport_Throws()
        => Building(m => m.Map(TestFields.Price, e => e.Price).Filterable([FilterOperator.Contains]))
            .Should().Throw<ArgumentException>().WithMessage("*does not support Contains*");

    [Fact]
    public void Filterable_NoOperators_Throws()
        => Building(m => m.Map(TestFields.Price, e => e.Price).Filterable([]))
            .Should().Throw<ArgumentException>();

    [Fact]
    public void Filterable_UnsupportedType_Throws()
    {
        var uri = new Field<TestField, Uri>(TestField.Name);

        Building(m => m.Map(uri, e => new Uri(e.Name)).Filterable()).Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void DefaultSort_OnAnUnmappedField_Throws()
        => Building(m => m.DefaultSort(TestFields.Price))
            .Should().Throw<InvalidOperationException>().WithMessage("*sorts by 'Price' by default, but does not map it*");

    [Fact]
    public void DefaultSort_Twice_Throws()
        => Building(m =>
        {
            m.Map(TestFields.Price, e => e.Price);
            m.DefaultSort(TestFields.Price).DefaultSort(TestFields.Price, SortDirection.Desc);
        }).Should().Throw<InvalidOperationException>().WithMessage("*'Price' by default more than once*");

    [Fact]
    public void SpatialFilter_SetTwice_Throws()
        => Building(m => m.SpatialFilter(_ => e => true).SpatialFilter(_ => e => true))
            .Should().Throw<InvalidOperationException>();

    // --- Registration ---------------------------------------------------------

    [Fact]
    public void AddModel_BuildsTheModelStraightAway()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddQorel().AddModel(new ConfiguredModel(_ => { }, withId: false));

        act.Should().Throw<InvalidOperationException>().WithMessage("*has no id*");
    }

    [Fact]
    public void AddModel_RegistersTheModelAsBothTypes()
    {
        var model = new TestQueryModel();
        ServiceProvider provider = new ServiceCollection().AddQorel().AddModel(model).Services.BuildServiceProvider();

        provider.GetRequiredService<QueryModel<TestEntity, TestField>>().Should().BeSameAs(model);
        provider.GetRequiredService<TestQueryModel>().Should().BeSameAs(model);
    }

    [Fact]
    public void AddModel_Twice_Throws()
    {
        QorelBuilder builder = new ServiceCollection().AddQorel().AddModel(new TestQueryModel());

        Action act = () => builder.AddModel(new TestQueryModel());

        act.Should().Throw<InvalidOperationException>().WithMessage("*already registered*");
    }

    [Fact]
    public void AddModel_ModelAlreadyUsed_Throws()
    {
        var model = new TestQueryModel();
        _ = model.ClientFields;

        Action act = () => new ServiceCollection().AddQorel().AddModel(model);

        act.Should().Throw<InvalidOperationException>().WithMessage("*already been built*");
    }

    [Fact]
    public void AddQorel_Twice_Throws()
    {
        var services = new ServiceCollection();
        services.AddQorel();

        Action act = () => services.AddQorel();

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(101, 100)]
    [InlineData(20, 0)]
    public void AddQorel_InvalidPageSizes_Throw(int defaultPageSize, int maxPageSize)
    {
        Action act = () => new ServiceCollection().AddQorel(o =>
        {
            o.DefaultPageSize = defaultPageSize;
            o.MaxPageSize = maxPageSize;
        });

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // --- Helpers --------------------------------------------------------------

    // Builds a model with an id plus whatever the test configures, unless the test leaves the id out.
    private static Action Building(Action<QueryModelBuilder<TestEntity, TestField>> configure, bool withId = true)
        => () => _ = new ConfiguredModel(configure, withId).ClientFields;

    private sealed class ConfiguredModel(Action<QueryModelBuilder<TestEntity, TestField>> configure, bool withId = true)
        : QueryModel<TestEntity, TestField>
    {
        protected override void Configure(QueryModelBuilder<TestEntity, TestField> model)
        {
            if (withId)
                model.Id(TestFields.Id, e => e.Id);

            configure(model);
        }
    }

    private interface IOperatorProbe
    {
        IReadOnlySet<FilterOperator> Operators { get; }
    }

    private sealed record Holder<T>(Guid Id, T Value);

    private sealed class SingleFieldModel<T> : QueryModel<Holder<T>, TestField>, IOperatorProbe
    {
        public IReadOnlySet<FilterOperator> Operators => ClientFields.Single().AllowedOperators;

        protected override void Configure(QueryModelBuilder<Holder<T>, TestField> model)
        {
            model.Id(TestFields.Id, e => e.Id);
            model.Map(new Field<TestField, T>(TestField.Name), e => e.Value).Filterable();
        }
    }
}
