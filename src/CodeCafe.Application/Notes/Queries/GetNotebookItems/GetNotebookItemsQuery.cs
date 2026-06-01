using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookItems;

public sealed record GetNotebookItemsQuery(
    Guid NotebookId,
    Guid CurrentUserId,
    string? Search,
    bool IncludeArchived = false) : IQuery<NotesResult<IReadOnlyList<NotebookItemModel>>>;
