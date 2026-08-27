using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetMyNotebooks;

public sealed record GetMyNotebooksQuery(
    Guid CurrentUserId,
    string? Search,
    int? Limit = null,
    int? Offset = null
) : IQuery<IReadOnlyList<NotebookSummaryModel>>;
