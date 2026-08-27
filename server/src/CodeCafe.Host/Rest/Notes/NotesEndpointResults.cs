using CodeCafe.Application.Notes;
using CodeCafe.Host.Common;

namespace CodeCafe.Host.Rest.Notes;

public static partial class NotesEndpoints
{
    private static int ToStatusCode(NotesFailureKind kind) =>
        NotesFailureStatusCodes.ToStatusCode(kind);

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
            result.Value!.Select(NotesEndpointMappings.ToItemResponse).ToList()
        );
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
        return TypedResults.Problem(
            ApiProblems.Create(error.Code, error.Message, ToStatusCode(error.Kind))
        );
    }
}
