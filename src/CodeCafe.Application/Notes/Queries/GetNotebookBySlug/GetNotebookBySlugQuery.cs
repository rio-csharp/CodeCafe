using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookBySlug;

public sealed record GetNotebookBySlugQuery(
    string Slug,
    Guid CurrentUserId,
    bool IncludeArchived = false) : IQuery<NotesResult<NotebookDetailModel>>;
