using CodeCafe.Application.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace CodeCafe.Host.Hosting;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // The client already disconnected; writing a response body would fail on the broken
        // connection and nobody would read it anyway.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var (statusCode, code, message, kind) = exception switch
        {
            NotImplementedException => (
                StatusCodes.Status501NotImplemented,
                "not_implemented",
                "This operation is not implemented yet.",
                ErrorKind.Unexpected),
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "validation_error",
                string.Join("; ", validationException.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}")),
                ErrorKind.Validation),
            _ => (
                StatusCodes.Status500InternalServerError,
                "internal_error",
                "An unexpected error occurred.",
                ErrorKind.Unexpected)
        };

        // 400/501 are expected outcomes; anything else is a bug and must be logged here
        // because Serilog's request logging sits outside this handler.
        if (exception is not (ValidationException or NotImplementedException))
        {
            logger.LogError(exception, "Request failed with an unhandled exception");
        }

        await ErrorResponses.WriteAsync(
            httpContext, statusCode, code, message, kind, cancellationToken);
        return true;
    }
}
