namespace CodeCafe.Application.Blocks.ApplyBlockOps;

// DryRun validates and returns the normalized ops without persisting anything (AI planning loop).
public sealed record ApplyBlockOpsRequest(IReadOnlyList<BlockOp> Ops, bool DryRun = false);
