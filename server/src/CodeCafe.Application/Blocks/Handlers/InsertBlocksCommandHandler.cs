using CodeCafe.Application.Blocks.Commands;
using CodeCafe.Application.Blocks.Models;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Blocks.Handlers;

public sealed class InsertBlocksCommandHandler : ICommandHandler<InsertBlocksCommand, Result<IReadOnlyList<BlockDto>>>
{
    public Task<Result<IReadOnlyList<BlockDto>>> Handle(InsertBlocksCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
