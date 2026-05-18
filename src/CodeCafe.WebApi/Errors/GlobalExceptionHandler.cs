using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;

namespace CodeCafe.WebApi.Errors;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
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
