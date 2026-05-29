using CodeCafe.WebApi.Errors;
using CodeCafe.WebApi.Mcp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CodeCafe.WebApi.Tests.Errors;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WhenMcpProtocolRequest_ReturnsFalse()
    {
        var handler = CreateHandler();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/mcp";
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new InvalidOperationException("boom"),
            CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    [Fact]
    public async Task TryHandleAsync_WhenClientCancelsRequest_Returns499()
    {
        var handler = CreateHandler();
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        using var requestAborted = new CancellationTokenSource();
        requestAborted.Cancel();
        httpContext.RequestAborted = requestAborted.Token;

        var handled = await handler.TryHandleAsync(
            httpContext,
            new OperationCanceledException(requestAborted.Token),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status499ClientClosedRequest, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_WhenServerSideCancellation_Returns500()
    {
        var handler = CreateHandler();
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new OperationCanceledException(),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(httpContext.Response.Body);
        Assert.Equal("internal_error", json.RootElement.GetProperty("code").GetString());
    }

    private static GlobalExceptionHandler CreateHandler()
    {
        var options = Options.Create(new McpOptions
        {
            Enabled = true,
            EndpointPath = "/mcp",
            ProtectedResourceMetadataPath = "/.well-known/oauth-protected-resource/mcp"
        });

        return new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, options);
    }
}
