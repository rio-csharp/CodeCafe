using CodeCafe.Application.Blocks.ApplyBlockOps;
using CodeCafe.Application.Blocks.DeleteBlock;
using CodeCafe.Application.Blocks.InsertBlocks;
using CodeCafe.Application.Blocks.MoveBlock;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Blocks.UpdateBlock;
using CodeCafe.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Host.Controllers;

[ApiController]
[Route("api")]
[Tags("Blocks")]
public sealed class BlocksController(ISender sender) : ControllerBase
{
    [HttpPost("pages/{pageId:guid}/blocks")]
    public Task<Result<IReadOnlyList<BlockDto>>> Insert(Guid pageId, InsertBlocksRequest request, CancellationToken cancellationToken)
        => sender.Send(new InsertBlocksCommand(pageId, request.AfterBlockId, request.Format, request.Blocks), cancellationToken);

    [HttpPost("pages/{pageId:guid}/blocks/batch")]
    public Task<Result<IReadOnlyList<BlockOpResultDto>>> ApplyBatch(Guid pageId, ApplyBlockOpsRequest request, CancellationToken cancellationToken)
        => sender.Send(new ApplyBlockOpsCommand(pageId, request.Ops), cancellationToken);

    [HttpPatch("pages/{pageId:guid}/blocks/{blockId:guid}")]
    public Task<Result<BlockDto>> Update(Guid pageId, Guid blockId, UpdateBlockRequest request, CancellationToken cancellationToken)
        => sender.Send(new UpdateBlockCommand(pageId, blockId, request.Content, request.BaseRevision), cancellationToken);

    [HttpDelete("pages/{pageId:guid}/blocks/{blockId:guid}")]
    public Task<Result> Delete(Guid pageId, Guid blockId, CancellationToken cancellationToken)
        => sender.Send(new DeleteBlockCommand(pageId, blockId), cancellationToken);

    [HttpPost("pages/{pageId:guid}/blocks/{blockId:guid}/move")]
    public Task<Result> Move(Guid pageId, Guid blockId, MoveBlockRequest request, CancellationToken cancellationToken)
        => sender.Send(new MoveBlockCommand(pageId, blockId, request.AfterBlockId), cancellationToken);
}
