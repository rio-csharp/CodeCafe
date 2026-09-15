using CodeCafe.Application.Blocks.Models;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Blocks.Commands;

public sealed record ApplyBlockOpsCommand(
    Guid PageId,
    IReadOnlyList<BlockOp> Ops) : ICommand<Result<IReadOnlyList<BlockOpResultDto>>>;
