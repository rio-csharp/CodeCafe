using CodeCafe.Shared.Application.Identity;
using CodeCafe.Modules.Notes.Application.Notes.Commands.CreateNotebook;
using CodeCafe.Modules.Notes.Application.Notes.Commands.DeleteNotebook;
using CodeCafe.Modules.Notes.Application.Notes.Commands.UpdateNotebook;
using CodeCafe.Modules.Notes.Application.Notes.Queries.GetMyNotebooks;
using CodeCafe.Modules.Notes.Application.Notes.Queries.GetNotebookById;
using CodeCafe.Modules.Notes.Application.Notes.Queries.GetNotebookBySlug;
using CodeCafe.Modules.Notes.Presentation.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Modules.Notes.Presentation.Endpoints.Notes;

public static partial class NotesEndpoints
{
    private const int DefaultNotebookListLimit = 50;
    private const int MaxNotebookListLimit = 100;

    private static void MapNotebookEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/mine", GetMyNotebooksAsync)
            .RequireAuthorization();
        group.MapGet("/{notebookId:guid}", GetNotebookByIdAsync)
            .AllowAnonymous();
        group.MapGet("/{slug}", GetNotebookBySlugAsync)
            .AllowAnonymous();
        group.MapPost("/", CreateNotebookAsync)
            .RequireAuthorization();
        group.MapPut("/{notebookId:guid}", UpdateNotebookAsync)
            .RequireAuthorization();
        group.MapDelete("/{notebookId:guid}", DeleteNotebookAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> GetMyNotebooksAsync(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        [FromQuery] string? search,
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        CancellationToken cancellationToken)
    {
        var notebooks = await sender.Send(
            new GetMyNotebooksQuery(
                currentUserAccessor.GetCurrentUserId() ?? Guid.Empty,
                search,
                NormalizeNotebookListLimit(limit),
                NormalizeNotebookListOffset(offset)),
            cancellationToken);

        return TypedResults.Ok<IReadOnlyList<NotebookSummaryResponse>>(
            notebooks.Select(NotesEndpointMappings.ToSummaryResponse).ToList());
    }

    private static async Task<IResult> GetNotebookByIdAsync(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        Guid notebookId,
        [FromQuery] bool? includeItems,
        [FromQuery] bool? includeContent,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetNotebookByIdQuery(
                notebookId,
                currentUserAccessor.GetCurrentUserId() ?? Guid.Empty,
                IncludeItems: includeItems ?? false,
                IncludeContent: includeContent ?? false),
            cancellationToken);

        return ToDetailResult(result);
    }

    private static async Task<IResult> GetNotebookBySlugAsync(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        string slug,
        [FromQuery] bool? includeItems,
        [FromQuery] bool? includeContent,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetNotebookBySlugQuery(
                slug,
                currentUserAccessor.GetCurrentUserId() ?? Guid.Empty,
                IncludeItems: includeItems ?? false,
                IncludeContent: includeContent ?? false),
            cancellationToken);

        return ToDetailResult(result);
    }

    private static async Task<IResult> CreateNotebookAsync(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        CreateNotebookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateNotebookCommand(
                currentUserAccessor.GetCurrentUserId() ?? Guid.Empty,
                request.Title,
                request.Description,
                request.Visibility),
            cancellationToken);

        if (!result.Succeeded)
        {
            return TypedResults.Problem(ApiProblems.Create(
                result.Error!.Code,
                result.Error.Message,
                ToStatusCode(result.Error.Kind)));
        }

        var response = NotesEndpointMappings.ToDetailResponse(result.Value!);
        return TypedResults.Created($"/api/notes/{response.Id}", response);
    }

    private static async Task<IResult> UpdateNotebookAsync(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        Guid notebookId,
        UpdateNotebookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateNotebookCommand(
                notebookId,
                currentUserAccessor.GetCurrentUserId() ?? Guid.Empty,
                request.Title,
                request.Description,
                request.Visibility),
            cancellationToken);

        return ToDetailResult(result);
    }

    private static int NormalizeNotebookListLimit(int? limit)
    {
        return Math.Clamp(limit ?? DefaultNotebookListLimit, 1, MaxNotebookListLimit);
    }

    private static int NormalizeNotebookListOffset(int? offset)
    {
        return Math.Max(0, offset ?? 0);
    }

    private static async Task<IResult> DeleteNotebookAsync(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        Guid notebookId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DeleteNotebookCommand(notebookId, currentUserAccessor.GetCurrentUserId() ?? Guid.Empty),
            cancellationToken);

        return ToCommandResult(result);
    }
}
