using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Blocks.InsertBlocks;

public sealed class InsertBlocksCommandHandler : ICommandHandler<InsertBlocksCommand, Result<IReadOnlyList<BlockDto>>>
{
    public Task<Result<IReadOnlyList<BlockDto>>> Handle(InsertBlocksCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
