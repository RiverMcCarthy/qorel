namespace Qorel.Tests;

public class QueryResultTests
{
    [Fact]
    public void Success_IsSuccess_IsTrue()
    {
        QueryResult<int>.Success(1).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Success_Value_ReturnsWrappedValue()
    {
        QueryResult<int>.Success(42).Value.Should().Be(42);
    }

    [Fact]
    public void Failure_IsFailure_IsTrue()
    {
        QueryResult<int>.Failure(new ValidationError("c", "m")).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Failure_Value_ThrowsInvalidOperationException()
    {
        Func<int> act = () => QueryResult<int>.Failure(new ValidationError("c", "m")).Value;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Failure_Errors_ContainsAllPassedErrors()
    {
        var result = QueryResult<int>.Failure(
            new ValidationError("c1", "first"),
            new ValidationError("c2", "second"));
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Map_OnSuccess_TransformsValue()
    {
        QueryResult<int>.Success(5).Map(x => x * 2).Value.Should().Be(10);
    }

    [Fact]
    public void Map_OnFailure_PropagatesErrorsWithoutCallingDelegate()
    {
        bool called = false;
        QueryResult<int> result = QueryResult<int>.Failure(new ValidationError("c", "m"))
            .Map(x => { called = true; return x; });

        result.IsFailure.Should().BeTrue();
        called.Should().BeFalse();
    }

    [Fact]
    public void Bind_OnSuccess_RunsNextStep()
    {
        QueryResult<int>.Success(5)
            .Bind(x => QueryResult<string>.Success($"value:{x}"))
            .Value.Should().Be("value:5");
    }

    [Fact]
    public void Bind_NextStepFails_ReturnsItsErrors()
    {
        QueryResult<string> result = QueryResult<int>.Success(5)
            .Bind(_ => QueryResult<string>.Failure(new ValidationError("next", "m")));

        result.Errors.Should().ContainSingle(e => e.Code == "next");
    }

    [Fact]
    public void Bind_OnFailure_PropagatesErrorsWithoutCallingNextStep()
    {
        bool called = false;
        QueryResult<string> result = QueryResult<int>.Failure(new ValidationError("first", "m"))
            .Bind(x => { called = true; return QueryResult<string>.Success("x"); });

        result.Errors.Should().ContainSingle(e => e.Code == "first");
        called.Should().BeFalse();
    }

    [Fact]
    public void Match_OnSuccess_CallsOnSuccess()
    {
        QueryResult<int>.Success(7).Match(v => $"ok:{v}", _ => "fail").Should().Be("ok:7");
    }

    [Fact]
    public void Match_OnFailure_CallsOnFailure()
    {
        QueryResult<int>.Failure(new ValidationError("c", "m"))
            .Match(_ => "ok", errs => $"fail:{errs.Count}")
            .Should().Be("fail:1");
    }

    [Fact]
    public void GetValueOrThrow_OnSuccess_ReturnsValue()
    {
        QueryResult<int>.Success(3).GetValueOrThrow().Should().Be(3);
    }

    [Fact]
    public void GetValueOrThrow_OnFailure_ThrowsWithEveryError()
    {
        Func<int> act = () => QueryResult<int>.Failure(
            new ValidationError("c1", "first error"),
            new ValidationError("c2", "second error")).GetValueOrThrow();

        act.Should().Throw<QueryValidationException>()
           .WithMessage("*first error*second error*")
           .Which.Errors.Select(e => e.Code).Should().Equal("c1", "c2");
    }

    [Fact]
    public void Failure_WithNoErrors_Throws()
    {
        Func<QueryResult<int>> act = () => QueryResult<int>.Failure([]);

        act.Should().Throw<ArgumentException>();
    }
}
