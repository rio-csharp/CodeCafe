using System.Text.Json;
using Microsoft.Extensions.Options;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace CodeCafe.Host.Hosting;

// Streams events as Server-Sent Events (text/event-stream). The first event is pulled
// before any headers are written so that failures thrown up front (e.g. NotImplementedException)
// still surface as normal HTTP error responses instead of a truncated stream.
internal sealed class SseResult<T>(IAsyncEnumerable<T> events, Func<T, string?> eventType) : IResult
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
            while (hasNext)
            {
                var current = enumerator.Current;
                var name = eventType(current);
                if (!string.IsNullOrEmpty(name))
                    await httpContext.Response.WriteAsync($"event: {name}\n", cancellationToken);

                var data = JsonSerializer.Serialize(current, jsonOptions);
                await httpContext.Response.WriteAsync($"data: {data}\n\n", cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);

                hasNext = await enumerator.MoveNextAsync();
            }
        }

        static void CommitHeaders(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/event-stream; charset=utf-8";
            context.Response.Headers.CacheControl = "no-cache";
        }
    }
}
