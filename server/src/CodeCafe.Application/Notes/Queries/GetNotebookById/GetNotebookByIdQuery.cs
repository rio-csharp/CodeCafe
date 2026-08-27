using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookById;

public sealed record GetNotebookByIdQuery(
    Guid NotebookId,
    Guid CurrentUserId,
    bool IncludeArchived = false,
    bool IncludeItems = true,
    bool IncludeContent = true
) : IQuery<NotesResult<NotebookDetailModel>>;
