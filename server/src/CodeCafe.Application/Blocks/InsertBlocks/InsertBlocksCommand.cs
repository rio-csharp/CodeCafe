using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Blocks.InsertBlocks;

public sealed record InsertBlocksCommand(
    Guid PageId,
    Guid? AfterBlockId,
    BlockContentFormat Format,
    IReadOnlyList<BlockInput> Blocks) : ICommand<Result<IReadOnlyList<BlockDto>>>;
