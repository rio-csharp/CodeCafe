using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Commands.RemoveNotebookFavorite;

public sealed record RemoveNotebookFavoriteCommand(
    Guid NotebookId,
    Guid CurrentUserId) : ICommand<NotesResult<NotebookFavoriteModel>>;
