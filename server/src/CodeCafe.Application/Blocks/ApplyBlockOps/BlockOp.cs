using System.Text.Json;

namespace CodeCafe.Application.Blocks.ApplyBlockOps;

// One operation in a batch. Which fields apply depends on Kind: Insert uses TempId/Type/Content
// (plus After/Parent for placement), Update uses BlockId/Content/BaseVersion, Delete uses BlockId,
// Move uses BlockId/After/Parent. Content is always the typed payload. BlockId, After and Parent
// are strings because they name either a live block's Guid or a TempId minted by an earlier op
// in the batch. Parent pins the target group explicitly: with Parent set, After is interpreted
// among that block's children (null After = first child) — the only way to land in a childless
// parent, since a bare After always joins the after-block's OWN group.
public sealed record BlockOp(
    BlockOpKind Kind,
    string? BlockId,
    string? TempId,
    string? Type,
    string? After,
    JsonElement? Content,
    long? BaseVersion,
    string? Parent = null);
