using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Queries.GetNotebookBySlug;

public sealed record GetNotebookBySlugQuery(
    string Slug,
    Guid CurrentUserId,
    bool IncludeArchived = false,
    bool IncludeItems = true,
    bool IncludeContent = true) : IQuery<NotesResult<NotebookDetailModel>>;
