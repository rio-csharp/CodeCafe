using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookById;

public sealed record GetNotebookByIdQuery(
    Guid NotebookId,
    Guid CurrentUserId,
    bool IncludeArchived = false) : IQuery<NotesResult<NotebookDetailModel>>;
