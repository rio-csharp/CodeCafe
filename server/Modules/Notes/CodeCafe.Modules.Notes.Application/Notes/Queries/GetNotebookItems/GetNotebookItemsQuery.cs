using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Queries.GetNotebookItems;

public sealed record GetNotebookItemsQuery(
    Guid NotebookId,
    Guid CurrentUserId,
    string? Search,
    bool IncludeArchived = false,
    bool IncludeContent = true) : IQuery<NotesResult<IReadOnlyList<NotebookItemModel>>>;
