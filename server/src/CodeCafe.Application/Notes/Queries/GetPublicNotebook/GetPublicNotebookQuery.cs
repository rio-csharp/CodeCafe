using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetPublicNotebook;

public sealed record GetPublicNotebookQuery(
    string Slug,
    Guid CurrentUserId,
    bool IncludeArchived = false,
    bool IncludeItems = true,
    bool IncludeContent = false
) : IQuery<NotesResult<NotebookDetailModel>>;
