using CodeCafe.Application.Blocks.Models;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Models;

namespace CodeCafe.Application.Pages.Commands;

public sealed record CreatePageCommand(
    string NotebookIdOrSlug,
    string Title,
    string? ParentPath,
    IReadOnlyList<BlockInput>? Blocks,
    BlockContentFormat? Format) : ICommand<Result<PageDetailsDto>>;
