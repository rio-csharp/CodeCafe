using CodeCafe.Application.Notes;
using CodeCafe.Application.Notes.Commands.ArchiveNotebookItem;
using CodeCafe.Application.Notes.Commands.CreateNotebookItem;
using CodeCafe.Application.Notes.Commands.DeleteNotebookItem;
using CodeCafe.Application.Notes.Commands.ReorderNotebookItems;
using CodeCafe.Application.Notes.Commands.RestoreNotebookItem;
using CodeCafe.Application.Notes.Commands.UpdateNotebookItem;
using CodeCafe.Application.Notes.Queries.GetNotebookItemById;
using CodeCafe.Application.Notes.Queries.GetNotebookItems;
using CodeCafe.Application.Common.Identity;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Host.Rest.Notes;

public static partial class NotesEndpoints
{
    private static void MapItemEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/{notebookId:guid}/items", GetNotebookItemsAsync)
            .AllowAnonymous();
        group.MapGet("/{notebookId:guid}/items/{itemId:guid}", GetNotebookItemByIdAsync)
            .AllowAnonymous();
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
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        Guid notebookId,
        [FromQuery] string? search,
        [FromQuery] bool? includeArchived,
        [FromQuery] bool? includeContent,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetNotebookItemsQuery(
                notebookId,
                currentUserAccessor.GetCurrentUserId() ?? Guid.Empty,
                search,
                includeArchived ?? false,
                includeContent ?? false),
            cancellationToken);

        return ToListResult(result);
    }

    private static async Task<IResult> GetNotebookItemByIdAsync(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        Guid notebookId,
        Guid itemId,
        [FromQuery] bool? includeArchived,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetNotebookItemByIdQuery(
                notebookId,
                itemId,
                currentUserAccessor.GetCurrentUserId() ?? Guid.Empty,
                includeArchived ?? false),
            cancellationToken);

        return ToItemResult(result);
    }

    private static async Task<IResult> CreateNotebookItemAsync(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        Guid notebookId,
        CreateNotebookItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateNotebookItemCommand(
                notebookId,
                currentUserAccessor.GetCurrentUserId() ?? Guid.Empty,
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
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        Guid notebookId,
        Guid itemId,
        UpdateNotebookItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateNotebookItemCommand(
                notebookId,
                itemId,
                currentUserAccessor.GetCurrentUserId() ?? Guid.Empty,
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
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        Guid notebookId,
        ReorderNotebookItemsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ReorderNotebookItemsCommand(
                notebookId,
                currentUserAccessor.GetCurrentUserId() ?? Guid.Empty,
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
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        Guid notebookId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ArchiveNotebookItemCommand(notebookId, itemId, currentUserAccessor.GetCurrentUserId() ?? Guid.Empty),
            cancellationToken);

        return ToItemResult(result);
    }

    private static async Task<IResult> RestoreNotebookItemAsync(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        Guid notebookId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RestoreNotebookItemCommand(notebookId, itemId, currentUserAccessor.GetCurrentUserId() ?? Guid.Empty),
            cancellationToken);

        return ToItemResult(result);
    }

    private static async Task<IResult> DeleteNotebookItemAsync(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        Guid notebookId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DeleteNotebookItemCommand(notebookId, itemId, currentUserAccessor.GetCurrentUserId() ?? Guid.Empty),
            cancellationToken);

        return ToCommandResult(result);
    }
}
