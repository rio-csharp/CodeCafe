using System.Text;
using CodeCafe.Host.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace CodeCafe.Host.Tests.Hosting;

public sealed class SseResultTests
{
    private sealed record TestEvent(string Kind, string? Message);

    [Fact]
    public async Task MidStreamFailure_WritesTerminalErrorFrame_AndDoesNotThrow()
    {
        var context = CreateContext();
        var result = new SseResult<TestEvent>(
            FailAfterFirst(),
            e => e.Kind,
            failureEvent: exception => new TestEvent("error", exception.Message)
        );

        await result.ExecuteAsync(context);

        var body = ResponseBody(context);
        Assert.Contains("event: text", body);
        Assert.Contains("event: error", body);
        Assert.Contains("boom", body);
    }

    [Fact]
    public async Task MidStreamFailure_WithoutFailureEvent_JustStops()
    {
        var context = CreateContext();
        var result = new SseResult<TestEvent>(FailAfterFirst(), e => e.Kind);

        await result.ExecuteAsync(context);

        var body = ResponseBody(context);
        Assert.Contains("event: text", body);
        Assert.DoesNotContain("event: error", body);
    }

    [Fact]
    public async Task ClientDisconnect_SwallowsQuietly()
    {
        var context = CreateContext();
        var result = new SseResult<TestEvent>(DisconnectAfterFirst(context), e => e.Kind);

        await result.ExecuteAsync(context);

        Assert.Contains("event: text", ResponseBody(context));
    }

    [Fact]
    public async Task Failure_BeforeHeaders_ThrowsNormally()
    {
        var context = CreateContext();
        var result = new SseResult<TestEvent>(FailImmediately(), e => e.Kind);

        await Assert.ThrowsAsync<InvalidOperationException>(() => result.ExecuteAsync(context));
        Assert.False(context.Response.HasStarted);
    }

    private static async IAsyncEnumerable<TestEvent> FailAfterFirst()
    {
        yield return new TestEvent("text", "hello");
        await Task.CompletedTask;
        throw new InvalidOperationException("boom");
    }

    private static async IAsyncEnumerable<TestEvent> DisconnectAfterFirst(DefaultHttpContext context)
    {
        yield return new TestEvent("text", "hello");
        await Task.CompletedTask;
        context.Abort(); // trips RequestAborted, mimicking a client that hung up
        throw new OperationCanceledException(context.RequestAborted);
    }

    private static async IAsyncEnumerable<TestEvent> FailImmediately()
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("boom");
#pragma warning disable CS0162 // Unreachable: the contract under test is "first pull throws".
        yield break;
#pragma warning restore CS0162
    }

    private static DefaultHttpContext CreateContext()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IOptions<HttpJsonOptions>>(Options.Create(new HttpJsonOptions()))
            .BuildServiceProvider();
        return new DefaultHttpContext
        {
            RequestServices = services,
            Response = { Body = new MemoryStream() },
        };
    }

    private static string ResponseBody(DefaultHttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEnd();
    }
}
