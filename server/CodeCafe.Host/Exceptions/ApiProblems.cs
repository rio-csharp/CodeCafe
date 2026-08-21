using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Host.Exceptions;

public static class ApiProblems
{
    public static ProblemDetails Create(string code, string detail, int statusCode, string? title = null)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title ?? GetDefaultTitle(statusCode),
            Detail = detail,
            Extensions = { ["code"] = code }
        };
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
            StatusCodes.Status500InternalServerError => "Internal Server Error",
            StatusCodes.Status504GatewayTimeout => "Gateway Timeout",
            _ => "Request Failed"
        };
    }
}
