using CodeCafe.Application.Notes.Commands.CreateNotebook;
using CodeCafe.Application.Notes.Queries.GetPublicNotebooks;
using CodeCafe.Application.Notes;
using CodeCafe.WebApi.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CodeCafe.WebApi.Notes;

[ApiController]
[Route("api/notes")]
public sealed class NotesController(
    ISender sender,
    INotebookQueryService notebookQueryService,
    INotebookCommandService notebookCommandService,
    INotebookFavoriteService notebookFavoriteService)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public")]
    [ProducesResponseType<IReadOnlyList<NotebookSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotebookSummaryResponse>>> GetPublicNotebooks(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var notebooks = await sender.Send(
            new GetPublicNotebooksQuery(search, GetCurrentUserId()),
            cancellationToken);
        return Ok(notebooks.Select(ToSummaryResponse).ToList());
    }

    [AllowAnonymous]
    [HttpGet("public/{slug}")]
    [ProducesResponseType<NotebookDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotebookDetailResponse>> GetPublicNotebook(
        string slug,
        CancellationToken cancellationToken)
    {
        return ToActionResult(
            await notebookQueryService.GetPublicNotebookAsync(slug, GetCurrentUserId(), cancellationToken),
            ToDetailResponse);
    }

    [AllowAnonymous]
    [HttpGet("public/{slug}/items")]
    [ProducesResponseType<IReadOnlyList<NotebookItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<NotebookItemResponse>>> GetPublicNotebookItems(
        string slug,
        CancellationToken cancellationToken)
    {
        return ToActionResult<IReadOnlyList<NotebookItemModel>, IReadOnlyList<NotebookItemResponse>>(
            await notebookQueryService.GetPublicNotebookItemsAsync(slug, cancellationToken),
            items => items.Select(ToItemResponse).ToList());
    }

    [AllowAnonymous]
    [HttpGet("public/{slug}/items/{**path}")]
    [ProducesResponseType<NotebookItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotebookItemResponse>> GetPublicNotebookItem(
        string slug,
        string path,
        CancellationToken cancellationToken)
    {
        return ToActionResult(
            await notebookQueryService.GetPublicNotebookItemAsync(slug, path, cancellationToken),
            ToItemResponse);
    }

    [Authorize]
    [HttpGet("mine")]
    [ProducesResponseType<IReadOnlyList<NotebookSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<NotebookSummaryResponse>>> GetMyNotebooks(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var notebooks = await notebookQueryService.GetMyNotebooksAsync(GetCurrentUserId(), search, cancellationToken);
        return Ok(notebooks.Select(ToSummaryResponse).ToList());
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType<NotebookDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NotebookDetailResponse>> CreateNotebook(
        CreateNotebookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateNotebookCommand(
                GetCurrentUserId(),
                request.Title,
                request.Description,
                request.Visibility),
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToFailureResult(result.Error!);
        }

        var response = ToDetailResponse(result.Value!);
        return CreatedAtAction(nameof(GetNotebook), new { notebookId = response.Id }, response);
    }

    [Authorize]
    [HttpGet("{notebookId:guid}")]
    [ProducesResponseType<NotebookDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotebookDetailResponse>> GetNotebook(
        Guid notebookId,
        CancellationToken cancellationToken)
    {
        return ToActionResult(
            await notebookQueryService.GetNotebookByIdAsync(notebookId, GetCurrentUserId(), cancellationToken),
            ToDetailResponse);
    }

    [AllowAnonymous]
    [HttpGet("{slug}")]
    [ProducesResponseType<NotebookDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotebookDetailResponse>> GetNotebookBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        return ToActionResult(
            await notebookQueryService.GetNotebookBySlugAsync(slug, GetCurrentUserId(), cancellationToken),
            ToDetailResponse);
    }

    [Authorize]
    [HttpPut("{notebookId:guid}")]
    [ProducesResponseType<NotebookDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotebookDetailResponse>> UpdateNotebook(
        Guid notebookId,
        UpdateNotebookRequest request,
        CancellationToken cancellationToken)
    {
        return ToActionResult(
            await notebookCommandService.UpdateNotebookAsync(
                notebookId,
                GetCurrentUserId(),
                request.Title,
                request.Description,
                request.Visibility,
                cancellationToken),
            ToDetailResponse);
    }

    [Authorize]
    [HttpGet("{notebookId:guid}/favorite")]
    [ProducesResponseType<NotebookFavoriteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotebookFavoriteResponse>> GetFavoriteStatus(
        Guid notebookId,
        CancellationToken cancellationToken)
    {
        return ToActionResult(
            await notebookFavoriteService.GetFavoriteStatusAsync(notebookId, GetCurrentUserId(), cancellationToken),
            ToFavoriteResponse);
    }

    [Authorize]
    [HttpPost("{notebookId:guid}/favorite")]
    [ProducesResponseType<NotebookFavoriteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotebookFavoriteResponse>> AddFavorite(
        Guid notebookId,
        CancellationToken cancellationToken)
    {
        return ToActionResult(
            await notebookFavoriteService.AddFavoriteAsync(notebookId, GetCurrentUserId(), cancellationToken),
            ToFavoriteResponse);
    }

    [Authorize]
    [HttpDelete("{notebookId:guid}/favorite")]
    [ProducesResponseType<NotebookFavoriteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotebookFavoriteResponse>> RemoveFavorite(
        Guid notebookId,
        CancellationToken cancellationToken)
    {
        return ToActionResult(
            await notebookFavoriteService.RemoveFavoriteAsync(notebookId, GetCurrentUserId(), cancellationToken),
            ToFavoriteResponse);
    }

    [Authorize]
    [HttpDelete("{notebookId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotebook(Guid notebookId, CancellationToken cancellationToken)
    {
        var result = await notebookCommandService.DeleteNotebookAsync(notebookId, GetCurrentUserId(), cancellationToken);
        return result.Succeeded ? NoContent() : ToFailureResult(result.Error!);
    }

    [AllowAnonymous]
    [HttpGet("{notebookId:guid}/items")]
    [ProducesResponseType<IReadOnlyList<NotebookItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<NotebookItemResponse>>> GetNotebookItems(
        Guid notebookId,
        [FromQuery] string? search,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        // Only the notebook owner can read archived items
        if (includeArchived)
        {
            var notebook = await notebookQueryService.GetNotebookByIdAsync(notebookId, currentUserId, cancellationToken);
            if (!notebook.Succeeded)
            {
                return ToFailureResult(notebook.Error!);
            }
            if (notebook.Value!.OwnerId != currentUserId)
            {
                return ToFailureResult(new NotesError(NotesFailureKind.Forbidden, "notebook_forbidden", "Only the notebook owner can view archived items."));
            }
        }

        return ToActionResult<IReadOnlyList<NotebookItemModel>, IReadOnlyList<NotebookItemResponse>>(
            await notebookQueryService.GetNotebookItemsAsync(notebookId, currentUserId, search, cancellationToken, includeArchived),
            items => items.Select(ToItemResponse).ToList());
    }

    [Authorize]
    [HttpPost("{notebookId:guid}/items")]
    [ProducesResponseType<NotebookItemResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotebookItemResponse>> CreateNotebookItem(
        Guid notebookId,
        CreateNotebookItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await notebookCommandService.CreateNotebookItemAsync(
            notebookId,
            GetCurrentUserId(),
            request.ParentId,
            request.Type,
            request.Title,
            request.SortOrder,
            request.ContentJson,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToFailureResult(result.Error!);
        }

        var response = ToItemResponse(result.Value!);
        return CreatedAtAction(nameof(GetNotebookItems), new { notebookId }, response);
    }

    [Authorize]
    [HttpPut("{notebookId:guid}/items/{itemId:guid}")]
    [ProducesResponseType<NotebookItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotebookItemResponse>> UpdateNotebookItem(
        Guid notebookId,
        Guid itemId,
        UpdateNotebookItemRequest request,
        CancellationToken cancellationToken)
    {
        return ToActionResult(
            await notebookCommandService.UpdateNotebookItemAsync(
                notebookId,
                itemId,
                GetCurrentUserId(),
                request.Title,
                request.ParentId,
                request.SortOrder,
                request.ContentJson,
                cancellationToken,
                request.ExpectedUpdatedAtUtc),
            ToItemResponse);
    }

    [Authorize]
    [HttpPut("{notebookId:guid}/items/reorder")]
    [ProducesResponseType<ReorderNotebookItemsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReorderNotebookItemsResponse>> ReorderNotebookItems(
        Guid notebookId,
        ReorderNotebookItemsRequest request,
        CancellationToken cancellationToken)
    {
        return ToActionResult(
            await notebookCommandService.ReorderNotebookItemsAsync(
                notebookId,
                GetCurrentUserId(),
                request.Items.Select(item => new ReorderNotebookItemModel(item.ItemId, item.ParentId, item.SortOrder)).ToList(),
                cancellationToken),
            items => new ReorderNotebookItemsResponse(items.Select(ToItemResponse).ToList()));
    }

    [Authorize]
    [HttpPost("{notebookId:guid}/items/{itemId:guid}/archive")]
    [ProducesResponseType<NotebookItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotebookItemResponse>> ArchiveNotebookItem(
        Guid notebookId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        return ToActionResult(
            await notebookCommandService.ArchiveNotebookItemAsync(notebookId, itemId, GetCurrentUserId(), cancellationToken),
            ToItemResponse);
    }

    [Authorize]
    [HttpPost("{notebookId:guid}/items/{itemId:guid}/restore")]
    [ProducesResponseType<NotebookItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotebookItemResponse>> RestoreNotebookItem(
        Guid notebookId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        return ToActionResult(
            await notebookCommandService.RestoreNotebookItemAsync(notebookId, itemId, GetCurrentUserId(), cancellationToken),
            ToItemResponse);
    }

    [Authorize]
    [HttpDelete("{notebookId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotebookItem(
        Guid notebookId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await notebookCommandService.DeleteNotebookItemAsync(notebookId, itemId, GetCurrentUserId(), cancellationToken);
        return result.Succeeded ? NoContent() : ToFailureResult(result.Error!);
    }

    private Guid GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(claimValue, out var userId)
            ? userId
            : Guid.Empty;
    }

    private ActionResult<TResponse> ToActionResult<TModel, TResponse>(
        NotesResult<TModel> result,
        Func<TModel, TResponse> map)
    {
        if (!result.Succeeded)
        {
            return ToFailureResult(result.Error!);
        }

        return Ok(map(result.Value!));
    }

    private ObjectResult ToFailureResult(NotesError error)
    {
        return error.Kind switch
        {
            NotesFailureKind.Validation => ProblemFactory.Result(StatusCodes.Status400BadRequest, error.Code, error.Message),
            NotesFailureKind.Forbidden => ProblemFactory.Result(StatusCodes.Status403Forbidden, error.Code, error.Message),
            NotesFailureKind.NotFound => ProblemFactory.Result(StatusCodes.Status404NotFound, error.Code, error.Message),
            NotesFailureKind.Conflict => ProblemFactory.Result(StatusCodes.Status409Conflict, error.Code, error.Message),
            _ => ProblemFactory.Result(StatusCodes.Status400BadRequest, error.Code, error.Message)
        };
    }

    private static NotebookSummaryResponse ToSummaryResponse(NotebookSummaryModel model)
    {
        return new NotebookSummaryResponse(
            model.Id,
            model.OwnerId,
            model.Title,
            model.Slug,
            model.Description,
            model.Visibility,
            model.IsPublished,
            model.AuthorDisplayName,
            model.CanEdit,
            model.ItemCount,
            model.FolderCount,
            model.PageCount,
            model.FavoriteCount,
            model.IsFavoritedByMe,
            model.LastActivityAtUtc,
            model.CreatedAtUtc,
            model.UpdatedAtUtc,
            model.PublishedAtUtc);
    }

    private static NotebookDetailResponse ToDetailResponse(NotebookDetailModel model)
    {
        return new NotebookDetailResponse(
            model.Id,
            model.OwnerId,
            model.Title,
            model.Slug,
            model.Description,
            model.Visibility,
            model.IsPublished,
            model.AuthorDisplayName,
            model.CanEdit,
            model.ItemCount,
            model.FolderCount,
            model.PageCount,
            model.FavoriteCount,
            model.IsFavoritedByMe,
            model.LastActivityAtUtc,
            model.CreatedAtUtc,
            model.UpdatedAtUtc,
            model.PublishedAtUtc,
            model.Items.Select(ToItemResponse).ToList());
    }

    private static NotebookFavoriteResponse ToFavoriteResponse(NotebookFavoriteModel model)
    {
        return new NotebookFavoriteResponse(model.NotebookId, model.IsFavorited, model.FavoriteCount);
    }

    private static NotebookItemResponse ToItemResponse(NotebookItemModel model)
    {
        return new NotebookItemResponse(
            model.Id,
            model.NotebookId,
            model.ParentId,
            model.Type,
            model.Title,
            model.Slug,
            model.Path,
            model.SortOrder,
            model.ContentFormat,
            model.ContentJson,
            model.PlainTextContent,
            model.IsArchived,
            model.ArchivedAtUtc,
            model.ArchivedByUserId,
            model.CreatedAtUtc,
            model.UpdatedAtUtc);
    }
}
