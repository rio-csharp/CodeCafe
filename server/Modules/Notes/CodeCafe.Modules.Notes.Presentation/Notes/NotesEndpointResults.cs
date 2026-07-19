using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Modules.Notes.Presentation.Errors;

namespace CodeCafe.Modules.Notes.Presentation.Endpoints.Notes;

public static partial class NotesEndpoints
{
    private static int ToStatusCode(NotesFailureKind kind)
    {
        return kind switch
        {
            NotesFailureKind.Validation => StatusCodes.Status400BadRequest,
            NotesFailureKind.Forbidden => StatusCodes.Status403Forbidden,
            NotesFailureKind.NotFound => StatusCodes.Status404NotFound,
            NotesFailureKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private static IResult ToDetailResult(NotesResult<NotebookDetailModel> result)
    {
        if (!result.Succeeded)
        {
            return ToProblemResult(result.Error!);
        }

        return TypedResults.Ok(NotesEndpointMappings.ToDetailResponse(result.Value!));
    }

    private static IResult ToListResult(NotesResult<IReadOnlyList<NotebookItemModel>> result)
    {
        if (!result.Succeeded)
        {
            return ToProblemResult(result.Error!);
        }

        return TypedResults.Ok<IReadOnlyList<NotebookItemResponse>>(
            result.Value!.Select(NotesEndpointMappings.ToItemResponse).ToList());
    }

    private static IResult ToItemResult(NotesResult<NotebookItemModel> result)
    {
        if (!result.Succeeded)
        {
            return ToProblemResult(result.Error!);
        }

        return TypedResults.Ok(NotesEndpointMappings.ToItemResponse(result.Value!));
    }

    private static IResult ToFavoriteResult(NotesResult<NotebookFavoriteModel> result)
    {
        if (!result.Succeeded)
        {
            return ToProblemResult(result.Error!);
        }

        return TypedResults.Ok(NotesEndpointMappings.ToFavoriteResponse(result.Value!));
    }

    private static IResult ToCommandResult(NotesResult result)
    {
        if (!result.Succeeded)
        {
            return ToProblemResult(result.Error!);
        }

        return TypedResults.NoContent();
    }

    private static IResult ToProblemResult(NotesError error)
    {
        return TypedResults.Problem(ApiProblems.Create(
            error.Code,
            error.Message,
            ToStatusCode(error.Kind)));
    }
}
