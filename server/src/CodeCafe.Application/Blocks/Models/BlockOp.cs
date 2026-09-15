using System.Text.Json;

namespace CodeCafe.Application.Blocks.Models;

public sealed record BlockOp(
    BlockOpKind Kind,
    Guid? BlockId,
    string? TempId,
    string? After,
    BlockContentFormat? Format,
    JsonElement? Content,
    long? BaseRevision);
