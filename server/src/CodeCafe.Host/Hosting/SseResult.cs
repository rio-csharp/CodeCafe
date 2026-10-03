using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace CodeCafe.Host.Hosting;

// Streams events as Server-Sent Events (text/event-stream). The first event is pulled
// before any headers are written so that failures thrown up front (e.g. NotImplementedException)
// still surface as normal HTTP error responses instead of a truncated stream.
//
// Once headers are committed the failure channel changes: no status code can be set anymore, so
// a mid-stream exception is logged and followed by a best-effort terminal error frame
// (failureEvent). Bubbling up instead would send the exception handler into a second response
// write, which crashes on top of the original failure and leaves the client with a silently
// truncated stream.
internal sealed class SseResult<T>(
    IAsyncEnumerable<T> events,
    Func<T, string?> eventType,
    Func<Exception, T>? failureEvent = null
) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;
        var jsonOptions = httpContext.RequestServices
            .GetRequiredService<IOptions<HttpJsonOptions>>().Value.SerializerOptions;

        var enumerator = events.GetAsyncEnumerator(cancellationToken);
        await using (enumerator.ConfigureAwait(false))
        {
            var hasNext = await enumerator.MoveNextAsync();

            CommitHeaders(httpContext);
            try
            {
                while (hasNext)
                {
                    await WriteFrameAsync(httpContext, enumerator.Current, jsonOptions, cancellationToken);
                    hasNext = await enumerator.MoveNextAsync();
                }
            }
            catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
            {
                // Client disconnected; nothing left to fix.
            }
            catch (Exception exception)
            {
                httpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(SseResult<T>))
                    .LogError(exception, "SSE stream failed mid-flight");
                if (failureEvent is not null)
                {
                    await TryWriteTerminalFrameAsync(httpContext, failureEvent(exception), jsonOptions);
                }
            }
        }

        static void CommitHeaders(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/event-stream; charset=utf-8";
            context.Response.Headers.CacheControl = "no-cache";
        }
    }

    private async Task WriteFrameAsync(
        HttpContext httpContext,
        T current,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken
    )
    {
        var name = eventType(current);
        if (!string.IsNullOrEmpty(name))
        {
            await httpContext.Response.WriteAsync($"event: {name}\n", cancellationToken);
        }

        var data = JsonSerializer.Serialize(current, jsonOptions);
        await httpContext.Response.WriteAsync($"data: {data}\n\n", cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }

    // The stream may be broken beyond repair (e.g. the client is already gone), so the terminal
    // frame is best-effort and untied from RequestAborted.
    private async Task TryWriteTerminalFrameAsync(HttpContext httpContext, T terminal, JsonSerializerOptions jsonOptions)
    {
        try
        {
            await WriteFrameAsync(httpContext, terminal, jsonOptions, CancellationToken.None);
        }
        catch
        {
            // Nothing can be delivered anymore; the exception is already logged above.
        }
    }
}
