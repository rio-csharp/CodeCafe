using CodeCafe.Application.Blocks.Shared;

namespace CodeCafe.Application.Pages.Shared;

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
    DateTimeOffset UpdatedAtUtc,
    // Page-level write access for the caller: notebook owner/editor, or an Editor share on the
    // page or any ancestor. False for anonymous readers.
    bool CanWrite);
