using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace Qorel.AspNetCore.Tests;

public class QueryResultHttpExtensionsTests
{
    private static readonly ValidationError[] _errors =
    [
        new(ErrorCodes.FieldNotSortable, "Field 'Category' cannot be used in a sort.", "Category"),
        new(ErrorCodes.FiltersTooMany, "A query can have at most 50 filters."),
    ];

    [Fact]
    public void Success_IsOkWithTheValue()
    {
        Results<Ok<int>, QueryValidationProblem> response = QueryResult<int>.Success(42).ToHttpResult();

        response.Result.Should().BeOfType<Ok<int>>().Which.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_IsAProblemWithEveryError()
    {
        Results<Ok<int>, QueryValidationProblem> response = QueryResult<int>.Failure(_errors).ToHttpResult();

        QueryValidationProblem problem = response.Result.Should().BeOfType<QueryValidationProblem>().Subject;
        problem.Errors.Should().Equal(_errors);
        problem.ProblemDetails.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public void CustomSuccess_UsesTheGivenResult()
    {
        Results<Created<int>, QueryValidationProblem> response = QueryResult<int>.Success(7)
            .ToHttpResult(value => TypedResults.Created($"/items/{value}", value));

        response.Result.Should().BeOfType<Created<int>>().Which.Location.Should().Be("/items/7");
    }

    [Fact]
    public void CustomSuccess_StillReturnsTheProblemOnFailure()
        => QueryResult<int>.Failure(_errors)
            .ToHttpResult(value => TypedResults.Created($"/items/{value}", value))
            .Result.Should().BeOfType<QueryValidationProblem>();

    [Fact]
    public async Task Problem_IsWrittenAsProblemJson()
    {
        DefaultHttpContext context = HttpContextWithBody();

        await QueryResult<int>.Failure(_errors).ToHttpResult().ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        context.Response.ContentType.Should().StartWith("application/problem+json");

        using JsonDocument body = await ReadBodyAsync(context);
        body.RootElement.GetProperty("title").GetString().Should().Be("The query is invalid.");
        body.RootElement.GetProperty("status").GetInt32().Should().Be(422);

        JsonElement first = body.RootElement.GetProperty("errors")[0];
        first.GetProperty("code").GetString().Should().Be(ErrorCodes.FieldNotSortable);
        first.GetProperty("message").GetString().Should().Be("Field 'Category' cannot be used in a sort.");
        first.GetProperty("field").GetString().Should().Be("Category");
    }

    [Fact]
    public void Problem_DescribesItselfToOpenApi()
    {
        var builder = new RouteEndpointBuilder(_ => Task.CompletedTask, RoutePatternFactory.Parse("/"), order: 0);

        QueryValidationProblem.PopulateMetadata(typeof(QueryResultHttpExtensionsTests).GetMethod(nameof(Problem_DescribesItselfToOpenApi))!, builder);

        builder.Metadata.OfType<IProducesResponseTypeMetadata>().Should().ContainSingle(m =>
            m.StatusCode == StatusCodes.Status422UnprocessableEntity && m.ContentTypes.Contains("application/problem+json"));
    }

    private static DefaultHttpContext HttpContextWithBody() => new()
    {
        RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        Response = { Body = new MemoryStream() },
    };

    private static async Task<JsonDocument> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}
