using System.Text.Json;

namespace CodeCafe.Application.Blocks.ApplyBlockOps;

// One operation in a batch. Which fields apply depends on Kind: Insert uses TempId/Type/Content
// (plus After for placement), Update uses BlockId/Content/BaseVersion, Delete uses BlockId,
// Move uses BlockId/After. Content is always the typed payload. BlockId and After are strings
// because they name either a live block's Guid or a TempId minted by an earlier op in the batch.
public sealed record BlockOp(
    BlockOpKind Kind,
    string? BlockId,
    string? TempId,
    string? Type,
    string? After,
    JsonElement? Content,
    long? BaseVersion);
