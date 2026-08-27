using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Commands.RemoveNotebookFavorite;

public sealed record RemoveNotebookFavoriteCommand(Guid NotebookId, Guid CurrentUserId)
    : ICommand<NotesResult<NotebookFavoriteModel>>;
