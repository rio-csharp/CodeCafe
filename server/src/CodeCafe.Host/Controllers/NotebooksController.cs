using System.Text;
using CodeCafe.Application.Common;
using CodeCafe.Application.Notebooks.ChangeNotebookSlug;
using CodeCafe.Application.Notebooks.CreateNotebook;
using CodeCafe.Application.Notebooks.DeleteNotebook;
using CodeCafe.Application.Notebooks.ExportNotebook;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
using CodeCafe.Application.Notebooks.GetNotebookSlugAvailability;
using CodeCafe.Application.Notebooks.GetNotebookTree;
using CodeCafe.Application.Notebooks.ImportNotebook;
using CodeCafe.Application.Notebooks.ListNotebooks;
using CodeCafe.Application.Notebooks.RevokeNotebookShare;
using CodeCafe.Application.Notebooks.SetNotebookAccessCode;
using CodeCafe.Application.Notebooks.SetNotebookFavorite;
using CodeCafe.Application.Notebooks.SetNotebookTags;
using CodeCafe.Application.Notebooks.ShareNotebook;
using CodeCafe.Application.Notebooks.UpdateNotebook;
using CodeCafe.Application.Pages.CreatePage;
using CodeCafe.Application.Pages.GetPageByPath;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Application.Trash.ListTrashedPages;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Host.Hosting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Host.Controllers;

[ApiController]
[Route("api")]
[Tags("Notebooks")]
public sealed class NotebooksController(ISender sender) : ControllerBase
{
    [HttpPost("notebooks")]
    public Task<Result<NotebookDetailsDto>> Create(CreateNotebookRequest request, CancellationToken cancellationToken)
        => sender.Send(new CreateNotebookCommand(request.Title, request.Description, request.Slug, request.Visibility), cancellationToken);

    // Markdown payloads can be large, but not unbounded.
    [HttpPost("notebooks/import")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public Task<Result<NotebookDetailsDto>> Import(NotebookExportDto export, CancellationToken cancellationToken)
        => sender.Send(new ImportNotebookCommand(export), cancellationToken);

    [HttpGet("notebooks")]
    public Task<Result<PagedResult<NotebookSummaryDto>>> List(
        string? tag,
        bool? favorite,
        NotebookVisibility? visibility,
        string? search,
        NotebookSort? sort,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
        => sender.Send(new ListNotebooksQuery(tag, favorite, visibility, search, sort, page, pageSize), cancellationToken);

    [AllowAnonymous]
    [HttpGet("notebooks/{idOrSlug}")]
    public Task<Result<NotebookDetailsDto>> Details(string idOrSlug, string? accessCode, CancellationToken cancellationToken)
        => sender.Send(new GetNotebookDetailsQuery(idOrSlug, accessCode), cancellationToken);

    [HttpPatch("notebooks/{idOrSlug}")]
    public Task<Result<NotebookDetailsDto>> Update(string idOrSlug, UpdateNotebookRequest request, CancellationToken cancellationToken)
        => sender.Send(new UpdateNotebookCommand(idOrSlug, request.Title, request.Description, request.Visibility), cancellationToken);

    [HttpDelete("notebooks/{idOrSlug}")]
    public Task<Result> Delete(string idOrSlug, CancellationToken cancellationToken)
        => sender.Send(new DeleteNotebookCommand(idOrSlug), cancellationToken);

    [HttpPost("notebooks/{idOrSlug}/slug")]
    public Task<Result<NotebookDetailsDto>> ChangeSlug(string idOrSlug, ChangeNotebookSlugRequest request, CancellationToken cancellationToken)
        => sender.Send(new ChangeNotebookSlugCommand(idOrSlug, request.Slug), cancellationToken);

    // Advisory only, and deliberately authenticated: an anonymous probe would turn slug
    // existence into an enumeration oracle across every notebook, private ones included.
    [HttpGet("notebooks/slugs/{slug}")]
    public Task<Result<NotebookSlugAvailabilityDto>> SlugAvailability(string slug, CancellationToken cancellationToken)
        => sender.Send(new GetNotebookSlugAvailabilityQuery(slug), cancellationToken);

    [HttpPost("notebooks/{idOrSlug}/access-code")]
    public Task<Result> SetAccessCode(string idOrSlug, SetNotebookAccessCodeRequest request, CancellationToken cancellationToken)
        => sender.Send(new SetNotebookAccessCodeCommand(idOrSlug, request.AccessCode), cancellationToken);

    [HttpPost("notebooks/{idOrSlug}/favorite")]
    public Task<Result> SetFavorite(string idOrSlug, SetFavoriteRequest request, CancellationToken cancellationToken)
        => sender.Send(new SetNotebookFavoriteCommand(idOrSlug, request.IsFavorite), cancellationToken);

    [HttpPost("notebooks/{idOrSlug}/shares")]
    public Task<Result> Share(string idOrSlug, ShareRequest request, CancellationToken cancellationToken)
        => sender.Send(new ShareNotebookCommand(idOrSlug, request.Email, request.Role), cancellationToken);

    [HttpDelete("notebooks/{idOrSlug}/shares/{userId:guid}")]
    public Task<Result> RevokeShare(string idOrSlug, Guid userId, CancellationToken cancellationToken)
        => sender.Send(new RevokeNotebookShareCommand(idOrSlug, userId), cancellationToken);

    [HttpPut("notebooks/{idOrSlug}/tags")]
    public Task<Result> SetTags(string idOrSlug, SetTagsRequest request, CancellationToken cancellationToken)
        => sender.Send(new SetNotebookTagsCommand(idOrSlug, request.Tags), cancellationToken);

    [AllowAnonymous]
    [HttpGet("notebooks/{idOrSlug}/tree")]
    public Task<Result<NotebookTreeDto>> Tree(string idOrSlug, string? accessCode, CancellationToken cancellationToken)
        => sender.Send(new GetNotebookTreeQuery(idOrSlug, accessCode), cancellationToken);

    [HttpGet("notebooks/{idOrSlug}/trash")]
    public Task<Result<PagedResult<TrashedPageEntryDto>>> ListTrashedPages(string idOrSlug, int? page, int? pageSize, CancellationToken cancellationToken)
        => sender.Send(new ListTrashedPagesQuery(idOrSlug, page, pageSize), cancellationToken);

    // A real file download (text/markdown + Content-Disposition), not the JSON envelope.
    [AllowAnonymous]
    [HttpGet("notebooks/{idOrSlug}/export")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "text/markdown")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IResult> Export(string idOrSlug, string? accessCode, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ExportNotebookQuery(idOrSlug, accessCode), cancellationToken);
        // Results.File handles Content-Disposition encoding safely.
        return result.ToHttpResult(export => Results.File(
            Encoding.UTF8.GetBytes(export.Markdown),
            "text/markdown; charset=utf-8",
            export.FileName));
    }

    [HttpPost("notebooks/{idOrSlug}/pages")]
    public Task<Result<PageDetailsDto>> CreatePage(string idOrSlug, CreatePageRequest request, CancellationToken cancellationToken)
        => sender.Send(new CreatePageCommand(idOrSlug, request.Title, request.ParentPath), cancellationToken);

    [AllowAnonymous]
    [HttpGet("notebooks/{idOrSlug}/pages/by-path")]
    public Task<Result<PageDetailsDto>> PageByPath(string idOrSlug, string path, string? accessCode, CancellationToken cancellationToken)
        => sender.Send(new GetPageByPathQuery(idOrSlug, path, accessCode), cancellationToken);
}
