using CodeCafe.Application.Notes.Commands.CreateNotebook;
using CodeCafe.Application.Notes.Commands.DeleteNotebook;
using CodeCafe.Application.Notes.Commands.UpdateNotebook;
using CodeCafe.Application.Notes.Queries.GetMyNotebooks;
using CodeCafe.Application.Notes.Queries.GetNotebookById;
using CodeCafe.Application.Notes.Queries.GetNotebookBySlug;
using CodeCafe.Api.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Api.Endpoints.Notes;

public static partial class NotesEndpoints
{
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
}
