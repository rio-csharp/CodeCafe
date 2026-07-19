using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Queries.GetPublicNotebooks;

public sealed record GetPublicNotebooksQuery(
    string? Search,
    Guid CurrentUserId,
    int? Limit = null,
    int? Offset = null) : IQuery<IReadOnlyList<NotebookSummaryModel>>;
