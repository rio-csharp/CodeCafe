using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Host.Exceptions;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }


        var unknownProblem = ApiProblems.Create("internal_error", "An unexpected error occurred.", StatusCodes.Status500InternalServerError);
        await LogAndRespondAsync(httpContext, StatusCodes.Status500InternalServerError, unknownProblem, exception);
        return true;
    }

    private async ValueTask LogAndRespondAsync(HttpContext httpContext, int statusCode, ProblemDetails problem, Exception exception)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception.");
        }
        else
        {
            logger.LogWarning(exception, "Request failed with status code {StatusCode}.", statusCode);
        }

        httpContext.Response.StatusCode = statusCode;
        await Results.Problem(problem).ExecuteAsync(httpContext);
    }
}
