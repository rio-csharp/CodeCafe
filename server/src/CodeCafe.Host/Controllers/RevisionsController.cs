using CodeCafe.Application.Common;
using CodeCafe.Application.Revisions.RestoreBlockRevision;
using CodeCafe.Application.Revisions.RestorePageToRevision;
using CodeCafe.Application.Revisions.ListBlockRevisions;
using CodeCafe.Application.Revisions.ListPageRevisions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Host.Controllers;

[ApiController]
[Route("api")]
[Tags("Revisions")]
public sealed class RevisionsController(ISender sender) : ControllerBase
{
    [HttpGet("pages/{pageId:guid}/revisions")]
    public Task<Result<CursorPage<PageRevisionGroupDto>>> ListPageRevisions(Guid pageId, string? cursor, int? pageSize, CancellationToken cancellationToken)
        => sender.Send(new ListPageRevisionsQuery(pageId, cursor, pageSize), cancellationToken);

    [HttpPost("pages/{pageId:guid}/revisions/restore")]
    public Task<Result> RestorePage(Guid pageId, RestorePageRevisionRequest request, CancellationToken cancellationToken)
        => sender.Send(new RestorePageToRevisionCommand(pageId, request.AtUtc), cancellationToken);

    [HttpGet("pages/{pageId:guid}/blocks/{blockId:guid}/revisions")]
    public Task<Result<CursorPage<BlockRevisionDto>>> ListBlockRevisions(Guid pageId, Guid blockId, string? cursor, int? pageSize, CancellationToken cancellationToken)
        => sender.Send(new ListBlockRevisionsQuery(pageId, blockId, cursor, pageSize), cancellationToken);

    [HttpPost("pages/{pageId:guid}/blocks/{blockId:guid}/revisions/restore")]
    public Task<Result> RestoreBlock(Guid pageId, Guid blockId, RestoreBlockRevisionRequest request, CancellationToken cancellationToken)
        => sender.Send(new RestoreBlockRevisionCommand(pageId, blockId, request.Revision), cancellationToken);
}
