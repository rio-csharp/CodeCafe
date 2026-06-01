using CodeCafe.Application.Notes.Commands.CreateNotebook;
using CodeCafe.Application.Notes.Queries.GetMyNotebooks;
using CodeCafe.Application.Notes.Queries.GetNotebookById;
using CodeCafe.Application.Notes.Queries.GetNotebookBySlug;
using CodeCafe.Application.Notes.Queries.GetPublicNotebook;
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
        group.MapGet("/mine", GetMyNotebooksAsync)
            .RequireAuthorization();
        group.MapGet("/{notebookId:guid}", GetNotebookByIdAsync)
            .RequireAuthorization();
        group.MapGet("/{slug}", GetNotebookBySlugAsync);
        group.MapPost("/", CreateNotebookAsync)
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
            return TypedResults.Problem(
                detail: result.Error!.Message,
                statusCode: ToStatusCode(result.Error.Kind),
                title: result.Error.Code);
        }

        return TypedResults.Ok(NotesEndpointMappings.ToDetailResponse(result.Value!));
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
