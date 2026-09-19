namespace CodeCafe.Application.Blocks.ApplyBlockOps;

public sealed record ApplyBlockOpsRequest(IReadOnlyList<BlockOp> Ops);
