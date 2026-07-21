using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Queries.GetNotebookFavoriteStatus;

public sealed record GetNotebookFavoriteStatusQuery(
    Guid NotebookId,
    Guid CurrentUserId) : IQuery<NotesResult<NotebookFavoriteModel>>;
