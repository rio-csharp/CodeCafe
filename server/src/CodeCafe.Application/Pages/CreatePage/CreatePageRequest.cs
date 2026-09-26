namespace CodeCafe.Application.Pages.CreatePage;

public sealed record CreatePageRequest(
    string Title,
    string? ParentPath
    // Blocks land with the blocks slice; a page starts empty until then.
    // IReadOnlyList<BlockInput>? Blocks,
    // BlockContentFormat? Format
);
