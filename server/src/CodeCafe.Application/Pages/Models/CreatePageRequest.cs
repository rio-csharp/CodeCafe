using CodeCafe.Application.Blocks.Models;

namespace CodeCafe.Application.Pages.Models;

public sealed record CreatePageRequest(
    string Title,
    string? ParentPath,
    IReadOnlyList<BlockInput>? Blocks,
    BlockContentFormat? Format);
