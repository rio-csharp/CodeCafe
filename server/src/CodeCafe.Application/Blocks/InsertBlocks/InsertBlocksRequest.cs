using CodeCafe.Application.Blocks.Shared;
namespace CodeCafe.Application.Blocks.InsertBlocks;

public sealed record InsertBlocksRequest(
    Guid? AfterBlockId,
    BlockContentFormat Format,
    IReadOnlyList<BlockInput> Blocks);
