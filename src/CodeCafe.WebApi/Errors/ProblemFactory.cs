using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.WebApi.Errors;

public static class ProblemFactory
{
    public static ProblemDetails Create(int statusCode, string code, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = code,
            Detail = detail,
            Type = $"https://codes.cafe/errors/{code}"
        };
        problem.Extensions["code"] = code;
        return problem;
    }

    public static ObjectResult Result(int statusCode, string code, string detail)
    {
        return new ObjectResult(Create(statusCode, code, detail))
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" },
        };
    }
}
