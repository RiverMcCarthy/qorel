using Microsoft.Extensions.DependencyInjection;

using Qorel.Tests.Fixtures;

namespace Qorel.Tests;

/// <summary>
/// Checking client filters against a model with <see cref="QueryModel{TEntity,TField}.ParseFilters(string?)"/>.
/// </summary>
public class ParseFiltersTests
{
    private readonly TestQueryModel _model = new();

    private QueryResult<FilterSet<TestField>> Parse(string json) => _model.ParseFilters(json);

    private QueryResult<FilterSet<TestField>> ParseOne(string field, string op, string value)
        => Parse($$"""[{ "field": "{{field}}", "operator": "{{op}}", "value": {{value}} }]""");

    // --- Valid input ----------------------------------------------------------

    [Fact]
    public void ValidFilters_AreTyped()
    {
        QueryResult<FilterSet<TestField>> result = Parse("""
            [
              { "field": "Price", "operator": "LessThan", "value": 5000 },
              { "field": "name", "operator": "contains", "value": "pro" }
            ]
            """);

        result.IsSuccess.Should().BeTrue();
        result.Value.CacheKey.Should().Be("""Name.Contains="pro";Price.LessThan=5000""");
    }

    [Fact]
    public void FieldKey_IsAcceptedForOlderClients()
        => Parse("""[{ "fieldKey": "Price", "operator": "Equals", "value": 1 }]""").IsSuccess.Should().BeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    [InlineData("[]")]
    public void NoFilters_GivesTheEmptySet(string? json)
        => _model.ParseFilters(json).Value.Should().BeSameAs(FilterSet<TestField>.Empty);

    [Theory]
    [InlineData("\"Alpha\"")]
    [InlineData("\"alpha\"")]
    [InlineData("\"ALPHA\"")]
    public void EnumValues_IgnoreCase(string value)
        => ParseOne("Category", "Equals", value).Value.Single().CacheKey.Should().Be("Category.Equals=\"Alpha\"");

    [Theory]
    [InlineData("""{ "field": "Score", "operator": "Equals", "value": 3.5 }""", "Score.Equals=3.5")]
    [InlineData("""{ "field": "Score", "operator": "Equals", "value": null }""", "Score.Equals=null")]
    [InlineData("""{ "field": "Score", "operator": "IsNull" }""", "Score.IsNull")]
    [InlineData("""{ "field": "Score", "operator": "IsNotNull", "value": null }""", "Score.IsNotNull")]
    [InlineData("""{ "field": "Score", "operator": "IsOneOf", "value": [2, null] }""", "Score.IsOneOf=[2,null]")]
    public void NullableFields_AcceptNulls(string filter, string expectedKey)
        => Parse($"[{filter}]").Value.CacheKey.Should().Be(expectedKey);

    // --- Which fields a client can use ----------------------------------------

    [Fact]
    public void ServerOnlyField_IsReportedAsUnknown()
        => ParseOne("OwnerId", "Equals", $"\"{Guid.NewGuid()}\"").Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FieldUnknown);

    [Fact]
    public void SortableOnlyField_IsNotFilterable()
        => ParseOne("Stock", "Equals", "1").Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FieldNotFilterable);

    [Fact]
    public void OperatorTheFieldDoesNotAllow_IsOperatorNotAllowed()
        => ParseOne("Price", "Contains", "\"1\"").Errors.Should().ContainSingle(e => e.Code == ErrorCodes.OperatorNotAllowed);

    [Fact]
    public void EveryError_IsReturned()
    {
        QueryResult<FilterSet<TestField>> result = Parse("""
            [
              { "field": "OwnerId", "operator": "Equals", "value": "x" },
              { "field": "Stock", "operator": "Equals", "value": 1 },
              { "field": "Price", "operator": "Equals", "value": "x" }
            ]
            """);

        result.Errors.Select(e => e.Code).Should().Equal(ErrorCodes.FieldUnknown, ErrorCodes.FieldNotFilterable, ErrorCodes.FilterValueInvalid);
    }

    // --- Values are read strictly ---------------------------------------------

    [Theory]
    [InlineData("Price", "Equals", "\"5\"")]             // a string for a number
    [InlineData("Price", "Equals", "null")]              // a null for a non-nullable number
    [InlineData("Name", "Equals", "5")]                  // a number for a string
    [InlineData("Category", "Equals", "1")]              // an enum by number
    [InlineData("Category", "Equals", "\"1\"")]          // an enum by number in a string
    [InlineData("Category", "Equals", "\"Alpha, Beta\"")] // a combination of members
    [InlineData("Category", "Equals", "\"Delta\"")]      // a member that does not exist
    [InlineData("CreatedAt", "Equals", "\"yesterday\"")] // not a date
    [InlineData("Price", "IsOneOf", "5")]                // IsOneOf without an array
    [InlineData("Price", "IsOneOf", "[1, \"2\"]")]       // one bad item in an array
    public void ValueOfTheWrongShape_IsFilterValueInvalid(string field, string op, string value)
        => ParseOne(field, op, value).Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FilterValueInvalid);

    [Fact]
    public void MissingValue_IsFilterValueInvalid()
        => Parse("""[{ "field": "Price", "operator": "Equals" }]""").Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FilterValueInvalid);

    // --- Malformed JSON -------------------------------------------------------

    [Theory]
    [InlineData("[{ not json")]
    [InlineData("""{ "field": "Price", "operator": "Equals", "value": 1 }""")] // an object, not an array
    [InlineData("[null]")]
    [InlineData("""[{ "field": "Price", "operator": "Nope", "value": 1 }]""")]
    [InlineData("""[{ "field": "Price", "operator": "1", "value": 1 }]""")]     // an operator by number
    [InlineData("""[{ "field": 5, "operator": "Equals", "value": 1 }]""")]      // a field that is not a string
    [InlineData("""[{ "operator": "Equals", "value": 1 }]""")]                  // no field
    public void MalformedJson_IsFiltersMalformed(string json)
        => Parse(json).Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FiltersMalformed);

    [Fact]
    public void UnknownFieldName_IsFiltersMalformed()
        => Parse("""[{ "field": "Nope", "operator": "Equals", "value": 1 }]""").Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FiltersMalformed);

    // --- Limits ---------------------------------------------------------------

    [Fact]
    public void TooManyFilters_IsRejected()
    {
        TestQueryModel model = ModelWith(o => o.MaxFilters = 2);
        string filter = """{ "field": "Price", "operator": "GreaterThan", "value": 1 }""";

        model.ParseFilters($"[{filter},{filter},{filter}]").Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FiltersTooMany);
    }

    [Fact]
    public void TooManyValues_IsRejected()
    {
        TestQueryModel model = ModelWith(o => o.MaxFilterValues = 2);

        model.ParseFilters("""[{ "field": "Price", "operator": "IsOneOf", "value": [1, 2, 3] }]""")
            .Errors.Should().ContainSingle(e => e.Code == ErrorCodes.FilterTooManyValues && e.Field == "Price");
    }

    private static TestQueryModel ModelWith(Action<QorelOptions> configure)
    {
        var model = new TestQueryModel();
        new ServiceCollection().AddQorel(configure).AddModel(model);
        return model;
    }
}
