using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetFavoriteNotebooks;

public sealed record GetFavoriteNotebooksQuery(
    Guid CurrentUserId,
    string? Search = null,
    int? Limit = null,
    int? Offset = null
) : IQuery<IReadOnlyList<NotebookSummaryModel>>;
