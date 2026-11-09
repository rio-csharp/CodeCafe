using System.Text.Json;

namespace CodeCafe.Application.Blocks.ApplyBlockOps;

// One operation in a batch. BlockId, After and Parent are strings because they name either a
// live block's Guid or a TempId minted by an earlier op in the batch. Which fields each Kind
// reads is the handler's switch; placement (After vs Parent) is BlockPositions.ResolveInsertion.
public sealed record BlockOp(
    BlockOpKind Kind,
    string? BlockId,
    string? TempId,
    string? Type,
    string? After,
    JsonElement? Content,
    long? BaseVersion,
    string? Parent = null);
