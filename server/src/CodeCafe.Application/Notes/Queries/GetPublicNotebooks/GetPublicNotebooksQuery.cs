using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetPublicNotebooks;

public sealed record GetPublicNotebooksQuery(
    string? Search,
    Guid CurrentUserId,
    int? Limit = null,
    int? Offset = null
) : IQuery<IReadOnlyList<NotebookSummaryModel>>;
