using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookBySlug;

public sealed record GetNotebookBySlugQuery(
    string Slug,
    Guid CurrentUserId,
    bool IncludeArchived = false,
    bool IncludeItems = true,
    bool IncludeContent = false,
    string? AccessCode = null
) : IQuery<NotesResult<NotebookDetailModel>>;
