using CodeCafe.Application.Notes;
using CodeCafe.Application.Notes.Commands.ArchiveNotebookItem;
using CodeCafe.Application.Notes.Commands.CreateNotebookItem;
using CodeCafe.Application.Notes.Commands.DeleteNotebookItem;
using CodeCafe.Application.Notes.Commands.ReorderNotebookItems;
using CodeCafe.Application.Notes.Commands.RestoreNotebookItem;
using CodeCafe.Application.Notes.Commands.UpdateNotebookItem;
using CodeCafe.Application.Notes.Queries.GetNotebookItems;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Api.Endpoints.Notes;

public static partial class NotesEndpoints
{
    private static void MapItemEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/{notebookId:guid}/items", GetNotebookItemsAsync);
        group.MapPost("/{notebookId:guid}/items", CreateNotebookItemAsync)
            .RequireAuthorization();
        group.MapPut("/{notebookId:guid}/items/reorder", ReorderNotebookItemsAsync)
            .RequireAuthorization();
        group.MapPut("/{notebookId:guid}/items/{itemId:guid}", UpdateNotebookItemAsync)
            .RequireAuthorization();
        group.MapPost("/{notebookId:guid}/items/{itemId:guid}/archive", ArchiveNotebookItemAsync)
            .RequireAuthorization();
        group.MapPost("/{notebookId:guid}/items/{itemId:guid}/restore", RestoreNotebookItemAsync)
            .RequireAuthorization();
        group.MapDelete("/{notebookId:guid}/items/{itemId:guid}", DeleteNotebookItemAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> GetNotebookItemsAsync(
        [FromServices] ISender sender,
        Guid notebookId,
        [FromQuery] string? search,
        [FromQuery] bool? includeArchived,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetNotebookItemsQuery(
                notebookId,
                GetCurrentUserId(httpContext.User),
                search,
                includeArchived ?? false),
            cancellationToken);

        return ToListResult(result);
    }

    private static async Task<IResult> CreateNotebookItemAsync(
        [FromServices] ISender sender,
        Guid notebookId,
        CreateNotebookItemRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateNotebookItemCommand(
                notebookId,
                GetCurrentUserId(httpContext.User),
                request.ParentId,
                request.Type,
                request.Title,
                request.SortOrder,
                request.ContentJson),
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToProblemResult(result.Error!);
        }

        return TypedResults.Created(
            $"/api/notes/{notebookId}/items/{result.Value!.Id}",
            NotesEndpointMappings.ToItemResponse(result.Value!));
    }

    private static async Task<IResult> UpdateNotebookItemAsync(
        [FromServices] ISender sender,
        Guid notebookId,
        Guid itemId,
        UpdateNotebookItemRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateNotebookItemCommand(
                notebookId,
                itemId,
                GetCurrentUserId(httpContext.User),
                request.Title,
                request.ParentId,
                request.SortOrder,
                request.ContentJson,
                request.ExpectedUpdatedAtUtc),
            cancellationToken);

        return ToItemResult(result);
    }

    private static async Task<IResult> ReorderNotebookItemsAsync(
        [FromServices] ISender sender,
        Guid notebookId,
        ReorderNotebookItemsRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ReorderNotebookItemsCommand(
                notebookId,
                GetCurrentUserId(httpContext.User),
                request.Items.Select(item => new ReorderNotebookItemModel(item.ItemId, item.ParentId, item.SortOrder)).ToList()),
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToProblemResult(result.Error!);
        }

        return TypedResults.Ok(new ReorderNotebookItemsResponse(
            result.Value!.Select(NotesEndpointMappings.ToItemResponse).ToList()));
    }

    private static async Task<IResult> ArchiveNotebookItemAsync(
        [FromServices] ISender sender,
        Guid notebookId,
        Guid itemId,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ArchiveNotebookItemCommand(notebookId, itemId, GetCurrentUserId(httpContext.User)),
            cancellationToken);

        return ToItemResult(result);
    }

    private static async Task<IResult> RestoreNotebookItemAsync(
        [FromServices] ISender sender,
        Guid notebookId,
        Guid itemId,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RestoreNotebookItemCommand(notebookId, itemId, GetCurrentUserId(httpContext.User)),
            cancellationToken);

        return ToItemResult(result);
    }

    private static async Task<IResult> DeleteNotebookItemAsync(
        [FromServices] ISender sender,
        Guid notebookId,
        Guid itemId,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DeleteNotebookItemCommand(notebookId, itemId, GetCurrentUserId(httpContext.User)),
            cancellationToken);

        return ToCommandResult(result);
    }
}
