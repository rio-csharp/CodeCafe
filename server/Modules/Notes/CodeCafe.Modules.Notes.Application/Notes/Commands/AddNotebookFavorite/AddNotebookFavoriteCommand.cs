using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Commands.AddNotebookFavorite;

public sealed record AddNotebookFavoriteCommand(
    Guid NotebookId,
    Guid CurrentUserId) : ICommand<NotesResult<NotebookFavoriteModel>>;
