using CodeCafe.Application.Blocks.ApplyBlockOps;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Blocks.ApplyBlockOps;

public sealed class ApplyBlockOpsCommandHandler : ICommandHandler<ApplyBlockOpsCommand, Result<IReadOnlyList<BlockOpResultDto>>>
{
    public Task<Result<IReadOnlyList<BlockOpResultDto>>> Handle(ApplyBlockOpsCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
