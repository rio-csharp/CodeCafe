using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Queries.GetNotebookItemById;

public sealed record GetNotebookItemByIdQuery(
    Guid NotebookId,
    Guid ItemId,
    Guid CurrentUserId,
    bool IncludeArchived = false) : IQuery<NotesResult<NotebookItemModel>>;
