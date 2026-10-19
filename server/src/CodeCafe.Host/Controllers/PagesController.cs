using System.Text;
using CodeCafe.Application.Common;
using CodeCafe.Application.Pages.ChangePageSlug;
using CodeCafe.Application.Pages.DeletePage;
using CodeCafe.Application.Pages.ExportPage;
using CodeCafe.Application.Pages.GetPage;
using CodeCafe.Application.Pages.ListFavoritePages;
using CodeCafe.Application.Pages.MovePage;
using CodeCafe.Application.Pages.RevokePageShare;
using CodeCafe.Application.Pages.SetPageFavorite;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Application.Pages.SharePage;
using CodeCafe.Application.Pages.UpdatePage;
using CodeCafe.Host.Hosting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeCafe.Host.Controllers;

[ApiController]
[Route("api")]
[Tags("Pages")]
public sealed class PagesController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("pages/{pageId:guid}")]
    [EnableRateLimiting(RateLimiterExtensions.AccessCodePolicy)]
    public Task<Result<PageDetailsDto>> Details(Guid pageId, CancellationToken cancellationToken)
        => sender.Send(new GetPageQuery(pageId, AccessCodeHeader.Read(Request)), cancellationToken);

    [HttpPatch("pages/{pageId:guid}")]
    public Task<Result<PageDetailsDto>> Update(Guid pageId, UpdatePageRequest request, CancellationToken cancellationToken)
        => sender.Send(new UpdatePageCommand(pageId, request.Title, request.IsArchived), cancellationToken);

    // Separate from Update: a slug the caller picked is theirs to fix on a clash, while one
    // derived from the title may be re-keyed, and that policy belongs to this operation alone.
    [HttpPost("pages/{pageId:guid}/slug")]
    public Task<Result<PageDetailsDto>> ChangeSlug(Guid pageId, ChangePageSlugRequest request, CancellationToken cancellationToken)
        => sender.Send(new ChangePageSlugCommand(pageId, request.Slug), cancellationToken);

    [HttpPost("pages/{pageId:guid}/move")]
    public Task<Result<PageDetailsDto>> Move(Guid pageId, MovePageRequest request, CancellationToken cancellationToken)
        => sender.Send(new MovePageCommand(pageId, request.ParentPath, request.AfterPageId), cancellationToken);

    [HttpDelete("pages/{pageId:guid}")]
    public Task<Result> Delete(Guid pageId, CancellationToken cancellationToken)
        => sender.Send(new DeletePageCommand(pageId), cancellationToken);

    // A real file download (text/markdown + Content-Disposition), not the JSON envelope.
    [AllowAnonymous]
    [HttpGet("pages/{pageId:guid}/export")]
    [EnableRateLimiting(RateLimiterExtensions.AccessCodePolicy)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "text/markdown")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IResult> Export(Guid pageId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ExportPageQuery(pageId, AccessCodeHeader.Read(Request)), cancellationToken);
        // Results.File handles Content-Disposition encoding safely.
        return result.ToHttpResult(export => Results.File(
            Encoding.UTF8.GetBytes(export.Markdown),
            "text/markdown; charset=utf-8",
            export.FileName));
    }

    [HttpGet("pages/favorites")]
    public Task<Result<IReadOnlyList<FavoritePageDto>>> Favorites(Guid? notebookId, CancellationToken cancellationToken)
        => sender.Send(new ListFavoritePagesQuery(notebookId), cancellationToken);

    [HttpPost("pages/{pageId:guid}/favorite")]
    [EnableRateLimiting(RateLimiterExtensions.AccessCodePolicy)]
    public Task<Result> SetFavorite(Guid pageId, SetFavoriteRequest request, CancellationToken cancellationToken)
        => sender.Send(new SetPageFavoriteCommand(pageId, request.IsFavorite, AccessCodeHeader.Read(Request)), cancellationToken);

    [HttpPost("pages/{pageId:guid}/shares")]
    [EnableRateLimiting(RateLimiterExtensions.SharePolicy)]
    public Task<Result> Share(Guid pageId, ShareRequest request, CancellationToken cancellationToken)
        => sender.Send(new SharePageCommand(pageId, request.Email, request.Role), cancellationToken);

    [HttpDelete("pages/{pageId:guid}/shares/{userId:guid}")]
    [EnableRateLimiting(RateLimiterExtensions.SharePolicy)]
    public Task<Result> RevokeShare(Guid pageId, Guid userId, CancellationToken cancellationToken)
        => sender.Send(new RevokePageShareCommand(pageId, userId), cancellationToken);
}
