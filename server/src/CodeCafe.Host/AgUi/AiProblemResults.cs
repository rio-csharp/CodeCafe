using CodeCafe.Application.Ai;
using CodeCafe.Application.Notes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Host.AgUi;

/// <summary>
/// The one place the AI REST transport turns an application failure into a problem-details response.
/// This used to live in AiHelpers inside the application layer, which meant a use-case handler could
/// not be compiled without AspNetCore.
/// </summary>
internal static class AiProblemResults
{
    public static IResult ToNotesError(NotesError error)
        => ToError(error.Code, error.Message, ToStatusCode(AiFlowError.ToAiFailureKind(error.Kind)), error.Field, error.Details);

    public static IResult ToError(AiFlowError error)
        => ToError(error.Code, error.Message, ToStatusCode(error.Kind), error.Field, error.Details);

    public static IResult ToError(
        string code,
        string message,
        int statusCode,
        string? field = null,
        IReadOnlyDictionary<string, object?>? details = null)
    {
        var problem = new ProblemDetails
        {
            Title = code,
            Detail = message,
            Status = statusCode
        };
        problem.Extensions["code"] = code;
        problem.Extensions["retryable"] = statusCode is StatusCodes.Status429TooManyRequests
            or StatusCodes.Status502BadGateway
            or StatusCodes.Status504GatewayTimeout;
        if (!string.IsNullOrWhiteSpace(field))
        {
            problem.Extensions["field"] = field;
        }

        if (details is not null)
        {
            problem.Extensions["details"] = details;
        }

        return TypedResults.Problem(problem);
    }

    /// <summary>
    /// The single AiFailureKind to HTTP status mapping for this transport.
    /// </summary>
    public static int ToStatusCode(AiFailureKind kind)
        => kind switch
        {
            AiFailureKind.Validation => StatusCodes.Status400BadRequest,
            AiFailureKind.Forbidden => StatusCodes.Status403Forbidden,
            AiFailureKind.NotFound => StatusCodes.Status404NotFound,
            AiFailureKind.Conflict => StatusCodes.Status409Conflict,
            AiFailureKind.Unprocessable => StatusCodes.Status422UnprocessableEntity,
            AiFailureKind.RateLimited => StatusCodes.Status429TooManyRequests,
            AiFailureKind.Upstream => StatusCodes.Status502BadGateway,
            AiFailureKind.Timeout => StatusCodes.Status504GatewayTimeout,
            _ => StatusCodes.Status400BadRequest
        };
}
