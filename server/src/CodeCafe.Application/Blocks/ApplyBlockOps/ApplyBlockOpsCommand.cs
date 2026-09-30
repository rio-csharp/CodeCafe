using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Blocks.ApplyBlockOps;

public sealed record ApplyBlockOpsCommand(
    Guid PageId,
    IReadOnlyList<BlockOp> Ops,
    bool DryRun = false) : ICommand<Result<IReadOnlyList<BlockOpResultDto>>>;
