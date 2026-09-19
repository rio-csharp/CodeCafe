using CodeCafe.Application.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace CodeCafe.Host.Hosting;

internal sealed class GlobalExceptionHandler : IExceptionHandler
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

        await ErrorResponses.WriteAsync(
            httpContext, statusCode, code, message, kind, cancellationToken);
        return true;
    }
}
