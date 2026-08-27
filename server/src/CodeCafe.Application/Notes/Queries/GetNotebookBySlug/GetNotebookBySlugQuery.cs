using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookBySlug;

public sealed record GetNotebookBySlugQuery(
    string Slug,
    Guid CurrentUserId,
    bool IncludeArchived = false,
    bool IncludeItems = true,
    bool IncludeContent = true
) : IQuery<NotesResult<NotebookDetailModel>>;
