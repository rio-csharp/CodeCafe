using CodeCafe.Shared.Application.Identity;
using CodeCafe.Modules.Notes.Application.Notes.Commands.AddNotebookFavorite;
using CodeCafe.Modules.Notes.Application.Notes.Commands.RemoveNotebookFavorite;
using CodeCafe.Modules.Notes.Application.Notes.Queries.GetNotebookFavoriteStatus;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Modules.Notes.Presentation.Endpoints.Notes;

public static partial class NotesEndpoints
{
    private static void MapFavoriteEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/{notebookId:guid}/favorite", GetFavoriteStatusAsync)
            .RequireAuthorization();
        group.MapPost("/{notebookId:guid}/favorite", AddFavoriteAsync)
            .RequireAuthorization();
        group.MapDelete("/{notebookId:guid}/favorite", RemoveFavoriteAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> GetFavoriteStatusAsync(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        Guid notebookId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetNotebookFavoriteStatusQuery(notebookId, currentUserAccessor.GetCurrentUserId() ?? Guid.Empty),
            cancellationToken);

        return ToFavoriteResult(result);
    }

    private static async Task<IResult> AddFavoriteAsync(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        Guid notebookId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddNotebookFavoriteCommand(notebookId, currentUserAccessor.GetCurrentUserId() ?? Guid.Empty),
            cancellationToken);

        return ToFavoriteResult(result);
    }

    private static async Task<IResult> RemoveFavoriteAsync(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        Guid notebookId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RemoveNotebookFavoriteCommand(notebookId, currentUserAccessor.GetCurrentUserId() ?? Guid.Empty),
            cancellationToken);

        return ToFavoriteResult(result);
    }
}
