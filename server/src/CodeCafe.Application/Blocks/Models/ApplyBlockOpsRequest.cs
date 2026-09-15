namespace CodeCafe.Application.Blocks.Models;

public sealed record ApplyBlockOpsRequest(IReadOnlyList<BlockOp> Ops);
