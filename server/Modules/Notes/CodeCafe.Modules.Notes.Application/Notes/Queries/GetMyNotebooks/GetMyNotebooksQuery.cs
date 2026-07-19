using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Queries.GetMyNotebooks;

public sealed record GetMyNotebooksQuery(
    Guid CurrentUserId,
    string? Search,
    int? Limit = null,
    int? Offset = null) : IQuery<IReadOnlyList<NotebookSummaryModel>>;
