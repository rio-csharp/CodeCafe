using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.CreatePage;

public sealed record CreatePageCommand(
    string NotebookIdOrSlug,
    string Title,
    string? ParentPath,
    IReadOnlyList<BlockInput>? Blocks,
    BlockContentFormat? Format) : ICommand<Result<PageDetailsDto>>;
