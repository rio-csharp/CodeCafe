using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Commands.AddNotebookFavorite;

public sealed record AddNotebookFavoriteCommand(Guid NotebookId, Guid CurrentUserId)
    : ICommand<NotesResult<NotebookFavoriteModel>>;
