using CodeCafe.Application.Common;
using CodeCafe.Application.Pages.DeletePage;
using CodeCafe.Application.Pages.MovePage;
using CodeCafe.Application.Pages.SetPageFavorite;
using CodeCafe.Application.Pages.SharePage;
using CodeCafe.Application.Pages.UpdatePage;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Application.Pages.GetPage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Host.Controllers;

[ApiController]
[Route("api")]
[Tags("Pages")]
public sealed class PagesController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("pages/{pageId:guid}")]
    public Task<Result<PageDetailsDto>> Details(Guid pageId, CancellationToken cancellationToken)
        => sender.Send(new GetPageQuery(pageId), cancellationToken);

    [HttpPatch("pages/{pageId:guid}")]
    public Task<Result<PageDetailsDto>> Update(Guid pageId, UpdatePageRequest request, CancellationToken cancellationToken)
        => sender.Send(new UpdatePageCommand(pageId, request.Title, request.IsArchived), cancellationToken);

    [HttpPost("pages/{pageId:guid}/move")]
    public Task<Result<PageDetailsDto>> Move(Guid pageId, MovePageRequest request, CancellationToken cancellationToken)
        => sender.Send(new MovePageCommand(pageId, request.ParentPath, request.AfterPageId), cancellationToken);

    [HttpDelete("pages/{pageId:guid}")]
    public Task<Result> Delete(Guid pageId, CancellationToken cancellationToken)
        => sender.Send(new DeletePageCommand(pageId), cancellationToken);

    [HttpPost("pages/{pageId:guid}/favorite")]
    public Task<Result> SetFavorite(Guid pageId, SetFavoriteRequest request, CancellationToken cancellationToken)
        => sender.Send(new SetPageFavoriteCommand(pageId, request.IsFavorite), cancellationToken);

    [HttpPost("pages/{pageId:guid}/shares")]
    public Task<Result> Share(Guid pageId, ShareRequest request, CancellationToken cancellationToken)
        => sender.Send(new SharePageCommand(pageId, request.Email, request.Role), cancellationToken);

    [HttpDelete("pages/{pageId:guid}/shares/{userId:guid}")]
    public Task<Result> RevokeShare(Guid pageId, Guid userId, CancellationToken cancellationToken)
        => sender.Send(new RevokePageShareCommand(pageId, userId), cancellationToken);
}
