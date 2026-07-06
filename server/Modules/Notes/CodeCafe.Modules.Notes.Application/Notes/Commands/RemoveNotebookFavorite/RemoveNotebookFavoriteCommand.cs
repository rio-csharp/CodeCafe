using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.RemoveNotebookFavorite;

public sealed record RemoveNotebookFavoriteCommand(
    Guid NotebookId,
    Guid CurrentUserId) : ICommand<NotesResult<NotebookFavoriteModel>>;
