namespace Qorel.Tests;

/// <summary>
/// Filtering data that is already in memory with <see cref="QueryModel{TEntity,TField}.ApplyFilters(IEnumerable{TEntity}, IEnumerable{Filter{TField}})"/>.
/// There is no database here, which is the point.
/// </summary>
public class InMemoryFilterTests
{
    private sealed record Point(int Id, string? Name, double Distance);

    private enum PointField { Id, Name, Distance, Unmapped }

    private static class PointFields
    {
        public static readonly ComparableField<PointField, int> Id = new(PointField.Id);
        public static readonly TextField<PointField> Name = new(PointField.Name);
        public static readonly ComparableField<PointField, double> Distance = new(PointField.Distance);
        public static readonly ComparableField<PointField, int> Unmapped = new(PointField.Unmapped);
    }

    private sealed class PointModel(bool ignoreCase = false) : QueryModel<Point, PointField>
    {
        protected override void Configure(QueryModelBuilder<Point, PointField> model)
        {
            model.Id(PointFields.Id, p => p.Id);
            model.Map(PointFields.Distance, p => p.Distance);

            FieldBuilder<Point, PointField, string> name = model.Map(PointFields.Name, p => p.Name!);
            if (ignoreCase)
                name.IgnoreCase();
        }
    }

    private static readonly Point[] _points =
    [
        new(1, "Ridge Loop", 5.2),
        new(2, "River Walk", 12.0),
        new(3, null, 8.4),
        new(4, "Ridge Run", 21.1),
    ];

    private static int[] Ids(params Filter<PointField>[] filters) => Ids(new PointModel(), filters);

    private static int[] Ids(PointModel model, params Filter<PointField>[] filters)
        => [.. model.ApplyFilters(_points, filters).Select(p => p.Id)];

    [Fact]
    public void NoFilters_ReturnsTheSourceUnchanged()
        => new PointModel().ApplyFilters(_points, []).Should().BeSameAs(_points);

    [Fact]
    public void Filters_AreAndedTogether()
        => Ids(PointFields.Name.StartsWith("Ridge"), PointFields.Distance.LessThan(10)).Should().Equal(1);

    [Fact]
    public void TextOperator_SkipsNullsWithoutThrowing()
        => Ids(PointFields.Name.Contains("R")).Should().Equal(1, 2, 4);

    [Fact]
    public void TextMatching_IsCaseSensitive()
        => Ids(PointFields.Name.Contains("ridge")).Should().BeEmpty();

    [Fact]
    public void IsNull_MatchesNulls()
        => Ids(PointFields.Name.IsNull()).Should().Equal(3);

    [Fact]
    public void IsNotOneOf_ExcludesTheValues()
        => Ids(PointFields.Id.IsNotOneOf(1, 4)).Should().Equal(2, 3);

    [Fact]
    public void FilterSets_CanBeApplied()
        => Ids(new PointModel(), [.. FilterSet<PointField>.Create([PointFields.Distance.GreaterThanOrEquals(12)])]).Should().Equal(2, 4);

    [Fact]
    public void Predicates_CanBeReused()
    {
        var model = new PointModel();
        Filter<PointField> filter = PointFields.Distance.GreaterThan(6);

        Ids(model, filter).Should().Equal(2, 3, 4);
        Ids(model, filter).Should().Equal(2, 3, 4);
    }

    [Fact]
    public void IgnoreCase_MatchesAnyCase()
    {
        var model = new PointModel(ignoreCase: true);

        Ids(model, PointFields.Name.EqualTo("RIDGE LOOP")).Should().Equal(1);
        Ids(model, PointFields.Name.Contains("ridge")).Should().Equal(1, 4);
        Ids(model, PointFields.Name.IsOneOf("river walk", "RIDGE RUN")).Should().Equal(2, 4);
    }

    [Fact]
    public void IgnoreCase_StillSkipsNulls()
        => Ids(new PointModel(ignoreCase: true), PointFields.Name.EndsWith("")).Should().Equal(1, 2, 4);

    [Fact]
    public void NullFilterValue_NeverMatchesAComparison()
    {
        var score = new Field<PointField, string?>(PointField.Name);

        Ids(PointFields.Name.Contains(null!)).Should().BeEmpty();
        Ids(score.EqualTo(null)).Should().Equal(3);
    }

    [Fact]
    public void UnmappedField_Throws()
    {
        Func<int[]> act = () => Ids(PointFields.Unmapped.EqualTo(1));

        act.Should().Throw<InvalidOperationException>().WithMessage("*'Unmapped' is not mapped in PointModel*");
    }

    [Fact]
    public void FilterOfTheWrongValueType_Throws()
    {
        var wrongType = new ComparableField<PointField, long>(PointField.Id);

        Func<int[]> act = () => Ids(wrongType.EqualTo(1));

        act.Should().Throw<ArgumentException>().WithMessage("*compares Int64 values, but the field is mapped as Int32*");
    }
}
