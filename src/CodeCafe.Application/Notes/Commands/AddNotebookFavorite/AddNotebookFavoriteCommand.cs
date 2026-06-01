using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.AddNotebookFavorite;

public sealed record AddNotebookFavoriteCommand(
    Guid NotebookId,
    Guid CurrentUserId) : ICommand<NotesResult<NotebookFavoriteModel>>;
