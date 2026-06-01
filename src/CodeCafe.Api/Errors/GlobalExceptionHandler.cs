using FluentValidation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Api.Errors;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, error) = exception switch
        {
            AntiforgeryValidationException => (
                StatusCodes.Status400BadRequest,
                new ApiError("invalid_csrf_token", "The CSRF token is missing or invalid.")),
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                new ApiError("validation_error", validationException.Errors.FirstOrDefault()?.ErrorMessage ?? "One or more validation errors occurred.")),
            DbUpdateException => (
                StatusCodes.Status500InternalServerError,
                new ApiError("database_error", "A database error occurred.")),
            TimeoutException => (
                StatusCodes.Status504GatewayTimeout,
                new ApiError("timeout", "The request timed out.")),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested => (
                StatusCodes.Status499ClientClosedRequest,
                new ApiError("request_cancelled", "The request was cancelled.")),
            _ => (
                StatusCodes.Status500InternalServerError,
                new ApiError("internal_error", "An unexpected error occurred."))
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception.");
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }
}
