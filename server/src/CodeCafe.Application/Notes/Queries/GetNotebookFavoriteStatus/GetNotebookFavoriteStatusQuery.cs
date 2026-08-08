using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Queries.GetNotebookFavoriteStatus;

public sealed record GetNotebookFavoriteStatusQuery(
    Guid NotebookId,
    Guid CurrentUserId) : IQuery<NotesResult<NotebookFavoriteModel>>;
