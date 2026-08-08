using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookItemById;

public sealed record GetNotebookItemByIdQuery(
    Guid NotebookId,
    Guid ItemId,
    Guid CurrentUserId,
    bool IncludeArchived = false) : IQuery<NotesResult<NotebookItemModel>>;
