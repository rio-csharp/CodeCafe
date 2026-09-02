using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookItems;

public sealed record GetNotebookItemsQuery(
    Guid NotebookId,
    Guid CurrentUserId,
    string? Search,
    bool IncludeArchived = false,
    bool IncludeContent = false
) : IQuery<NotesResult<IReadOnlyList<NotebookItemModel>>>;
