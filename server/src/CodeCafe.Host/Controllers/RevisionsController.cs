using CodeCafe.Application.Common;
using CodeCafe.Application.Revisions.GetPageAtRevision;
using CodeCafe.Application.Revisions.ListBlockRevisions;
using CodeCafe.Application.Revisions.ListPageRevisions;
using CodeCafe.Application.Revisions.RestoreBlockRevision;
using CodeCafe.Application.Revisions.RestorePageToRevision;
using CodeCafe.Host.Hosting;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeCafe.Host.Controllers;

[ApiController]
[Route("api")]
[Tags("Revisions")]
public sealed class RevisionsController(ISender sender) : ControllerBase
{
    [HttpGet("pages/{pageId:guid}/revisions")]
    [EnableRateLimiting(RateLimiterExtensions.AccessCodePolicy)]
    public Task<Result<CursorPage<PageRevisionGroupDto>>> ListPageRevisions(Guid pageId, string? cursor, int? pageSize, CancellationToken cancellationToken)
        => sender.Send(new ListPageRevisionsQuery(pageId, cursor, pageSize, AccessCodeHeader.Read(Request)), cancellationToken);

    [HttpGet("pages/{pageId:guid}/revisions/at")]
    [EnableRateLimiting(RateLimiterExtensions.AccessCodePolicy)]
    public Task<Result<PageRevisionSnapshotDto>> GetPageAtRevision(Guid pageId, DateTimeOffset atUtc, CancellationToken cancellationToken)
        => sender.Send(new GetPageAtRevisionQuery(pageId, atUtc, AccessCodeHeader.Read(Request)), cancellationToken);

    [HttpPost("pages/{pageId:guid}/revisions/restore")]
    public Task<Result> RestorePage(Guid pageId, RestorePageRevisionRequest request, CancellationToken cancellationToken)
        => sender.Send(new RestorePageToRevisionCommand(pageId, request.AtUtc), cancellationToken);

    [HttpGet("pages/{pageId:guid}/blocks/{blockId:guid}/revisions")]
    [EnableRateLimiting(RateLimiterExtensions.AccessCodePolicy)]
    public Task<Result<CursorPage<BlockRevisionDto>>> ListBlockRevisions(Guid pageId, Guid blockId, string? cursor, int? pageSize, CancellationToken cancellationToken)
        => sender.Send(new ListBlockRevisionsQuery(pageId, blockId, cursor, pageSize, AccessCodeHeader.Read(Request)), cancellationToken);

    [HttpPost("pages/{pageId:guid}/blocks/{blockId:guid}/revisions/restore")]
    public Task<Result> RestoreBlock(Guid pageId, Guid blockId, RestoreBlockRevisionRequest request, CancellationToken cancellationToken)
        => sender.Send(new RestoreBlockRevisionCommand(pageId, blockId, request.BlockVersion), cancellationToken);
}
