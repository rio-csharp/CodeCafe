using CodeCafe.Application.Common.Identity;
using CodeCafe.Application.Notes.Queries.GetPublicNotebook;
using CodeCafe.Application.Notes.Queries.GetPublicNotebookItem;
using CodeCafe.Application.Notes.Queries.GetPublicNotebookItems;
using CodeCafe.Application.Notes.Queries.GetPublicNotebooks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Host.Rest.Notes;

public static partial class NotesEndpoints
{
    private static void MapPublicEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/public", GetPublicNotebooksAsync).AllowAnonymous();
        group.MapGet("/public/{slug}", GetPublicNotebookAsync).AllowAnonymous();
        group.MapGet("/public/{slug}/items", GetPublicNotebookItemsAsync).AllowAnonymous();
        group.MapGet("/public/{slug}/items/{**path}", GetPublicNotebookItemAsync).AllowAnonymous();
    }

    private static async Task<IResult> GetPublicNotebooksAsync(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        [FromQuery] string? search,
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        CancellationToken cancellationToken
    )
    {
        var notebooks = await sender.Send(
            new GetPublicNotebooksQuery(
                search,
                currentUserAccessor.GetCurrentUserId() ?? Guid.Empty,
                NormalizeNotebookListLimit(limit),
                NormalizeNotebookListOffset(offset)
            ),
            cancellationToken
        );

        return TypedResults.Ok<IReadOnlyList<NotebookSummaryResponse>>(
            notebooks.Select(NotesEndpointMappings.ToSummaryResponse).ToList()
        );
    }

    private static async Task<IResult> GetPublicNotebookAsync(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        string slug,
        [FromQuery] bool? includeItems,
        [FromQuery] bool? includeContent,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(
            new GetPublicNotebookQuery(
                slug,
                currentUserAccessor.GetCurrentUserId() ?? Guid.Empty,
                IncludeItems: includeItems ?? false,
                IncludeContent: includeContent ?? false
            ),
            cancellationToken
        );

        return ToDetailResult(result);
    }

    private static async Task<IResult> GetPublicNotebookItemsAsync(
        [FromServices] ISender sender,
        string slug,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(new GetPublicNotebookItemsQuery(slug), cancellationToken);
        return ToListResult(result);
    }

    private static async Task<IResult> GetPublicNotebookItemAsync(
        [FromServices] ISender sender,
        string slug,
        string path,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(
            new GetPublicNotebookItemQuery(slug, path),
            cancellationToken
        );
        return ToItemResult(result);
    }
}
