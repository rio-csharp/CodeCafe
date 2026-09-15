namespace CodeCafe.Application.Blocks.Models;

public sealed record InsertBlocksRequest(
    Guid? AfterBlockId,
    BlockContentFormat Format,
    IReadOnlyList<BlockInput> Blocks);
