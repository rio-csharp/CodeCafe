using CodeCafe.Application.Blocks.Shared;

namespace CodeCafe.Application.Pages.CreatePage;

public sealed record CreatePageRequest(
    string Title,
    string? ParentPath,
    IReadOnlyList<BlockInput>? Blocks,
    BlockContentFormat? Format);
