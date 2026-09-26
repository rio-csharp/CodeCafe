using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.CreatePage;

public sealed record CreatePageCommand(
    string NotebookIdOrSlug,
    string Title,
    string? ParentPath
    // Blocks land with the blocks slice; a page starts empty until then.
    // IReadOnlyList<BlockInput>? Blocks,
    // BlockContentFormat? Format
) : ICommand<Result<PageDetailsDto>>;
