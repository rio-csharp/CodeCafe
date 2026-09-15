using CodeCafe.Application.Blocks.Models;

namespace CodeCafe.Application.Pages.Models;

public sealed record PageDetailsDto(
    Guid Id,
    Guid NotebookId,
    string Title,
    string Path,
    bool IsArchived,
    bool IsFavorite,
    IReadOnlyList<PageShareDto> Shares,
    IReadOnlyList<BlockDto> Blocks,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
