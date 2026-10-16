using CodeCafe.Application.Blocks.Shared;

namespace CodeCafe.Application.Blocks.InsertBlocks;

public sealed record InsertBlocksRequest(
    Guid? AfterBlockId,
    IReadOnlyList<BlockInput> Blocks);
