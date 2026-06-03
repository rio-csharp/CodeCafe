using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetPublicNotebook;

public sealed record GetPublicNotebookQuery(
    string Slug,
    Guid CurrentUserId,
    bool IncludeArchived = false) : IQuery<NotesResult<NotebookDetailModel>>;
