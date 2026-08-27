using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Host.Common;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (
            exception is OperationCanceledException
            && httpContext.RequestAborted.IsCancellationRequested
        )
        {
            // The client already disconnected; writing a response body would fail
            // on the broken connection and nobody would read it anyway.
            return true;
        }

        var (statusCode, problem) = exception switch
        {
            AntiforgeryValidationException => (
                StatusCodes.Status400BadRequest,
                ApiProblems.Create(
                    "invalid_csrf_token",
                    "The CSRF token is missing or invalid.",
                    StatusCodes.Status400BadRequest
                )
            ),
            CurrentUserNotAuthenticatedException => (
                StatusCodes.Status401Unauthorized,
                ApiProblems.Create(
                    "authentication_required",
                    "Authentication is required to access this resource.",
                    StatusCodes.Status401Unauthorized
                )
            ),
            ValidationException validationException when validationException.Errors.Any() => (
                StatusCodes.Status400BadRequest,
                ApiProblems.CreateValidation(
                    "validation_error",
                    validationException.Errors.GroupBy(
                        error => error.PropertyName,
                        error => error.ErrorMessage
                    ),
                    StatusCodes.Status400BadRequest
                )
            ),
            ValidationException => (
                StatusCodes.Status400BadRequest,
                ApiProblems.Create(
                    "validation_error",
                    "One or more validation errors occurred.",
                    StatusCodes.Status400BadRequest
                )
            ),
            // Optimistic concurrency conflicts (stale revision on edit) are an
            // expected business outcome, not a server failure: 409, no Error log.
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                ApiProblems.Create(
                    "concurrency_conflict",
                    "The resource was modified by another request. Refresh and try again.",
                    StatusCodes.Status409Conflict
                )
            ),
            DbUpdateException => (
                StatusCodes.Status500InternalServerError,
                ApiProblems.Create(
                    "database_error",
                    "A database error occurred.",
                    StatusCodes.Status500InternalServerError
                )
            ),
            TimeoutException => (
                StatusCodes.Status504GatewayTimeout,
                ApiProblems.Create(
                    "timeout",
                    "The request timed out.",
                    StatusCodes.Status504GatewayTimeout
                )
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                ApiProblems.Create(
                    "internal_error",
                    "An unexpected error occurred.",
                    StatusCodes.Status500InternalServerError
                )
            ),
        };

        // The MediatR LoggingBehavior already logged exceptions that escaped a
        // handler (with request name and elapsed time) and marked them; skip
        // those here so one fault does not produce two Error entries.
        // Optimistic concurrency conflicts are expected business outcomes at
        // 409, so they are logged at Info rather than Error.
        if (
            statusCode == StatusCodes.Status500InternalServerError
            && !ExceptionLoggingMarker.IsMarkedAsLogged(exception)
        )
        {
            logger.LogError(exception, "Unhandled exception.");
        }
        else if (exception is DbUpdateConcurrencyException)
        {
            logger.LogInformation(
                exception,
                "Optimistic concurrency conflict. Path={Path}",
                httpContext.Request.Path
            );
        }

        httpContext.Response.StatusCode = statusCode;
        await Results.Problem(problem).ExecuteAsync(httpContext);
        return true;
    }
}
