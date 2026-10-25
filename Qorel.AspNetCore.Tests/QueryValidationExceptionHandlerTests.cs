using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Qorel.AspNetCore.Tests;

public class QueryValidationExceptionHandlerTests
{
    private readonly QueryValidationExceptionHandler _handler = new();

    [Fact]
    public async Task QueryValidationException_BecomesA422Problem()
    {
        DefaultHttpContext context = HttpContextWithBody();
        var exception = new QueryValidationException([new ValidationError(ErrorCodes.SortsTooMany, "Too many sorts.")]);

        bool handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);

        context.Response.Body.Position = 0;
        using JsonDocument body = await JsonDocument.ParseAsync(context.Response.Body);
        body.RootElement.GetProperty("errors")[0].GetProperty("code").GetString().Should().Be(ErrorCodes.SortsTooMany);
    }

    [Fact]
    public async Task OtherExceptions_AreLeftAlone()
        => (await _handler.TryHandleAsync(HttpContextWithBody(), new InvalidOperationException(), CancellationToken.None)).Should().BeFalse();

    private static DefaultHttpContext HttpContextWithBody() => new()
    {
        RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        Response = { Body = new MemoryStream() },
    };
}
