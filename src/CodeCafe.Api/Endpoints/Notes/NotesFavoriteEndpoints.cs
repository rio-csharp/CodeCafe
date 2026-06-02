using CodeCafe.Application.Notes.Queries.GetNotebookFavoriteStatus;
using CodeCafe.Application.Notes.Commands.AddNotebookFavorite;
using CodeCafe.Application.Notes.Commands.RemoveNotebookFavorite;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Api.Endpoints.Notes;

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
        Guid notebookId,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetNotebookFavoriteStatusQuery(notebookId, GetCurrentUserId(httpContext.User)),
            cancellationToken);

        return ToFavoriteResult(result);
    }

    private static async Task<IResult> AddFavoriteAsync(
        [FromServices] ISender sender,
        Guid notebookId,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddNotebookFavoriteCommand(notebookId, GetCurrentUserId(httpContext.User)),
            cancellationToken);

        return ToFavoriteResult(result);
    }

    private static async Task<IResult> RemoveFavoriteAsync(
        [FromServices] ISender sender,
        Guid notebookId,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RemoveNotebookFavoriteCommand(notebookId, GetCurrentUserId(httpContext.User)),
            cancellationToken);

        return ToFavoriteResult(result);
    }
}
