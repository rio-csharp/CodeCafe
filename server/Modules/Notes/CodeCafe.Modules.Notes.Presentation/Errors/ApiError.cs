using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Modules.Notes.Presentation.Errors;

public static class ApiProblems
{
    public static ProblemDetails Create(
        string code,
        string detail,
        int statusCode,
        string? title = null)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title ?? GetDefaultTitle(statusCode),
            Detail = detail,
            Extensions =
            {
                ["code"] = code
            }
        };
    }

    public static HttpValidationProblemDetails CreateValidation(
        string code,
        IEnumerable<IGrouping<string, string>> errors,
        int statusCode,
        string? detail = null,
        string? title = null)
    {
        var validationProblem = new HttpValidationProblemDetails(
            errors.ToDictionary(
                group => string.IsNullOrWhiteSpace(group.Key) ? "$" : group.Key,
                group => group.Where(message => !string.IsNullOrWhiteSpace(message)).Distinct().ToArray()))
        {
            Status = statusCode,
            Title = title ?? GetDefaultTitle(statusCode),
            Detail = detail ?? "One or more validation errors occurred."
        };
        validationProblem.Extensions["code"] = code;
        return validationProblem;
    }

    private static string GetDefaultTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            StatusCodes.Status429TooManyRequests => "Too Many Requests",
            StatusCodes.Status499ClientClosedRequest => "Client Closed Request",
            StatusCodes.Status500InternalServerError => "Internal Server Error",
            StatusCodes.Status504GatewayTimeout => "Gateway Timeout",
            _ => "Request Failed"
        };
    }
}
