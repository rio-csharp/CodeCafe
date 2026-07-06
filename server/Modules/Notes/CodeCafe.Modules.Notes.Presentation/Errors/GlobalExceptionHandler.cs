using FluentValidation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Api.Errors;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, problem) = exception switch
        {
            AntiforgeryValidationException => (
                StatusCodes.Status400BadRequest,
                ApiProblems.Create("invalid_csrf_token", "The CSRF token is missing or invalid.", StatusCodes.Status400BadRequest)),
            ValidationException validationException when validationException.Errors.Any() => (
                StatusCodes.Status400BadRequest,
                ApiProblems.CreateValidation(
                    "validation_error",
                    validationException.Errors.GroupBy(error => error.PropertyName, error => error.ErrorMessage),
                    StatusCodes.Status400BadRequest)),
            ValidationException => (
                StatusCodes.Status400BadRequest,
                ApiProblems.Create("validation_error", "One or more validation errors occurred.", StatusCodes.Status400BadRequest)),
            DbUpdateException => (
                StatusCodes.Status500InternalServerError,
                ApiProblems.Create("database_error", "A database error occurred.", StatusCodes.Status500InternalServerError)),
            TimeoutException => (
                StatusCodes.Status504GatewayTimeout,
                ApiProblems.Create("timeout", "The request timed out.", StatusCodes.Status504GatewayTimeout)),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested => (
                StatusCodes.Status499ClientClosedRequest,
                ApiProblems.Create("request_cancelled", "The request was cancelled.", StatusCodes.Status499ClientClosedRequest)),
            _ => (
                StatusCodes.Status500InternalServerError,
                ApiProblems.Create("internal_error", "An unexpected error occurred.", StatusCodes.Status500InternalServerError))
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception.");
        }

        httpContext.Response.StatusCode = statusCode;
        await Results.Problem(problem).ExecuteAsync(httpContext);
        return true;
    }
}
