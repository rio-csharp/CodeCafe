using System.Text.Json;

using CodeCafe.Application.Blocks.Shared;
namespace CodeCafe.Application.Blocks.ApplyBlockOps;

public sealed record BlockOp(
    BlockOpKind Kind,
    Guid? BlockId,
    string? TempId,
    string? After,
    BlockContentFormat? Format,
    JsonElement? Content,
    long? BaseRevision);
