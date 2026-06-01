using CodeCafe.Application.Notes.Commands.CreateNotebook;
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

    private static Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(claimValue, out var userId)
            ? userId
            : Guid.Empty;
    }
}
