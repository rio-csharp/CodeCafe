using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetMyNotebooks;

public sealed record GetMyNotebooksQuery(
    Guid CurrentUserId,
    string? Search) : IQuery<IReadOnlyList<NotebookSummaryModel>>;
