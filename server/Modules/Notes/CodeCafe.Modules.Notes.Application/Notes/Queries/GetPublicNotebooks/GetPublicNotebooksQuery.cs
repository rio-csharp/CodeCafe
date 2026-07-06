using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetPublicNotebooks;

public sealed record GetPublicNotebooksQuery(
    string? Search,
    Guid CurrentUserId) : IQuery<IReadOnlyList<NotebookSummaryModel>>;
