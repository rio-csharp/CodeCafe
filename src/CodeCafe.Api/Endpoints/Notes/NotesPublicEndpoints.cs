using CodeCafe.Application.Notes.Queries.GetPublicNotebook;
using CodeCafe.Application.Notes.Queries.GetPublicNotebookItem;
using CodeCafe.Application.Notes.Queries.GetPublicNotebookItems;
using CodeCafe.Application.Notes.Queries.GetPublicNotebooks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Api.Endpoints.Notes;

public static partial class NotesEndpoints
{
    private static void MapPublicEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/public", GetPublicNotebooksAsync)
            .AllowAnonymous();
        group.MapGet("/public/{slug}", GetPublicNotebookAsync)
            .AllowAnonymous();
        group.MapGet("/public/{slug}/items", GetPublicNotebookItemsAsync)
            .AllowAnonymous();
        group.MapGet("/public/{slug}/items/{**path}", GetPublicNotebookItemAsync)
            .AllowAnonymous();
    }

    private static async Task<IResult> GetPublicNotebooksAsync(
        [FromServices] ISender sender,
        [FromQuery] string? search,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var notebooks = await sender.Send(
            new GetPublicNotebooksQuery(search, GetCurrentUserId(httpContext.User)),
            cancellationToken);

        return TypedResults.Ok<IReadOnlyList<NotebookSummaryResponse>>(
            notebooks.Select(NotesEndpointMappings.ToSummaryResponse).ToList());
    }

    private static async Task<IResult> GetPublicNotebookAsync(
        [FromServices] ISender sender,
        string slug,
        [FromQuery] bool? includeItems,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetPublicNotebookQuery(
                slug,
                GetCurrentUserId(httpContext.User),
                IncludeItems: includeItems ?? true),
            cancellationToken);

        return ToDetailResult(result);
    }

    private static async Task<IResult> GetPublicNotebookItemsAsync(
        [FromServices] ISender sender,
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPublicNotebookItemsQuery(slug), cancellationToken);
        return ToListResult(result);
    }

    private static async Task<IResult> GetPublicNotebookItemAsync(
        [FromServices] ISender sender,
        string slug,
        string path,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPublicNotebookItemQuery(slug, path), cancellationToken);
        return ToItemResult(result);
    }
}
