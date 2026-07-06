using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.ArchiveNotebookItem;

public sealed record ArchiveNotebookItemCommand(
    Guid NotebookId,
    Guid ItemId,
    Guid CurrentUserId) : ICommand<NotesResult<NotebookItemModel>>;
