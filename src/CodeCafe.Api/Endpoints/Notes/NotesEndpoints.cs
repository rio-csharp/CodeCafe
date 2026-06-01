using CodeCafe.Application.Notes.Commands.CreateNotebook;
using CodeCafe.Application.Notes.Commands.CreateNotebookItem;
using CodeCafe.Application.Notes.Commands.DeleteNotebook;
using CodeCafe.Application.Notes.Commands.DeleteNotebookItem;
using CodeCafe.Application.Notes.Commands.ReorderNotebookItems;
using CodeCafe.Application.Notes.Commands.UpdateNotebook;
using CodeCafe.Application.Notes.Commands.UpdateNotebookItem;
using CodeCafe.Application.Notes.Commands.ArchiveNotebookItem;
using CodeCafe.Application.Notes.Commands.RestoreNotebookItem;
using CodeCafe.Application.Notes.Commands.AddNotebookFavorite;
using CodeCafe.Application.Notes.Commands.RemoveNotebookFavorite;
using CodeCafe.Application.Notes.Queries.GetMyNotebooks;
using CodeCafe.Application.Notes.Queries.GetNotebookById;
using CodeCafe.Application.Notes.Queries.GetNotebookBySlug;
using CodeCafe.Application.Notes.Queries.GetNotebookFavoriteStatus;
using CodeCafe.Application.Notes.Queries.GetNotebookItems;
using CodeCafe.Application.Notes.Queries.GetPublicNotebook;
using CodeCafe.Application.Notes.Queries.GetPublicNotebookItem;
using CodeCafe.Application.Notes.Queries.GetPublicNotebookItems;
using CodeCafe.Application.Notes.Queries.GetPublicNotebooks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CodeCafe.Application.Notes;

namespace CodeCafe.Api.Endpoints.Notes;

public static class NotesEndpoints
{
    public static IEndpointRouteBuilder MapNotesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/notes")
            .WithTags("Notes");

        group.MapGet("/public", GetPublicNotebooksAsync);
        group.MapGet("/public/{slug}", GetPublicNotebookAsync);
        group.MapGet("/public/{slug}/items", GetPublicNotebookItemsAsync);
        group.MapGet("/public/{slug}/items/{**path}", GetPublicNotebookItemAsync);
        group.MapGet("/mine", GetMyNotebooksAsync)
            .RequireAuthorization();
        group.MapGet("/{notebookId:guid}", GetNotebookByIdAsync)
            .RequireAuthorization();
        group.MapGet("/{slug}", GetNotebookBySlugAsync);
        group.MapPost("/", CreateNotebookAsync)
            .RequireAuthorization();
        group.MapPut("/{notebookId:guid}", UpdateNotebookAsync)
            .RequireAuthorization();
        group.MapDelete("/{notebookId:guid}", DeleteNotebookAsync)
            .RequireAuthorization();
        group.MapGet("/{notebookId:guid}/favorite", GetFavoriteStatusAsync)
            .RequireAuthorization();
        group.MapPost("/{notebookId:guid}/favorite", AddFavoriteAsync)
            .RequireAuthorization();
        group.MapDelete("/{notebookId:guid}/favorite", RemoveFavoriteAsync)
            .RequireAuthorization();
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

        return endpoints;
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

    private static async Task<IResult> CreateNotebookAsync(
        [FromServices] ISender sender,
        CreateNotebookRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateNotebookCommand(
                GetCurrentUserId(httpContext.User),
                request.Title,
                request.Description,
                request.Visibility),
            cancellationToken);

        if (!result.Succeeded)
        {
            return TypedResults.Problem(
                detail: result.Error!.Message,
                statusCode: ToStatusCode(result.Error.Kind),
                title: result.Error.Code);
        }

        var response = NotesEndpointMappings.ToDetailResponse(result.Value!);
        return TypedResults.Created($"/api/notes/{response.Id}", response);
    }

    private static async Task<IResult> GetPublicNotebookAsync(
        [FromServices] ISender sender,
        string slug,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetPublicNotebookQuery(slug, GetCurrentUserId(httpContext.User)),
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

    private static async Task<IResult> GetMyNotebooksAsync(
        [FromServices] ISender sender,
        [FromQuery] string? search,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var notebooks = await sender.Send(
            new GetMyNotebooksQuery(GetCurrentUserId(httpContext.User), search),
            cancellationToken);

        return TypedResults.Ok<IReadOnlyList<NotebookSummaryResponse>>(
            notebooks.Select(NotesEndpointMappings.ToSummaryResponse).ToList());
    }

    private static async Task<IResult> GetNotebookByIdAsync(
        [FromServices] ISender sender,
        Guid notebookId,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetNotebookByIdQuery(notebookId, GetCurrentUserId(httpContext.User)),
            cancellationToken);

        return ToDetailResult(result);
    }

    private static async Task<IResult> GetNotebookBySlugAsync(
        [FromServices] ISender sender,
        string slug,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetNotebookBySlugQuery(slug, GetCurrentUserId(httpContext.User)),
            cancellationToken);

        return ToDetailResult(result);
    }

    private static async Task<IResult> UpdateNotebookAsync(
        [FromServices] ISender sender,
        Guid notebookId,
        UpdateNotebookRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateNotebookCommand(
                notebookId,
                GetCurrentUserId(httpContext.User),
                request.Title,
                request.Description,
                request.Visibility),
            cancellationToken);

        return ToDetailResult(result);
    }

    private static async Task<IResult> DeleteNotebookAsync(
        [FromServices] ISender sender,
        Guid notebookId,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DeleteNotebookCommand(notebookId, GetCurrentUserId(httpContext.User)),
            cancellationToken);

        return ToCommandResult(result);
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
        return TypedResults.Problem(
            detail: error.Message,
            statusCode: ToStatusCode(error.Kind),
            title: error.Code);
    }

    private static Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(claimValue, out var userId)
            ? userId
            : Guid.Empty;
    }
}
