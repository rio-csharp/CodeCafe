using System.Text.Json;
using CodeCafe.Application.Common;
using Microsoft.Extensions.Options;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace CodeCafe.Host.Hosting;

// Every error this host emits — application errors, auth challenges, rate-limit rejections,
// unhandled exceptions — shares one envelope: the serialized shape of Result.Failure<T>.
internal static class ErrorResponses
{
    public static Task WriteAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        ErrorKind kind,
        CancellationToken cancellationToken = default)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(
            Result.Failure<object>(new Error(code, message, kind)),
            SerializerOptions(context),
            cancellationToken);
    }

    public static Task WriteAsync(HttpContext context, Error error, CancellationToken cancellationToken = default)
        => WriteAsync(context, ErrorStatusCodes.FromKind(error.Kind), error.Code, error.Message, error.Kind, cancellationToken);

    public static IResult ToResult(Error error) => new ErrorEnvelopeResult(error);

    // Uses the same JSON options as minimal APIs so the envelope matches MVC output
    // (camelCase, string enums).
    private static JsonSerializerOptions SerializerOptions(HttpContext context)
        => context.RequestServices.GetRequiredService<IOptions<HttpJsonOptions>>().Value.SerializerOptions;

    private sealed class ErrorEnvelopeResult(Error error) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
            => WriteAsync(httpContext, error, httpContext.RequestAborted);
    }
}

internal static class ResultHttpMapping
{
    // For endpoints whose success response is not the Result envelope (file download, SSE):
    // success maps to a custom IResult, failure maps to the shared error envelope.
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess)
        => result is { IsSuccess: true, Value: not null }
            ? onSuccess(result.Value)
            : ErrorResponses.ToResult(result.Error!);
}
